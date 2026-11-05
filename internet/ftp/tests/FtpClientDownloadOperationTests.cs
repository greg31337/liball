using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ftp.Tests;

public class FtpClientDownloadOperationTests
{
    [Test]
    public void PRET_announces_the_exact_RETR_before_passive_setup()
    {
        using TcpListener controlListener = StartListener();
        using TcpListener dataListener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = controlListener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            Receive(stream, commands, "200 Type selected\r\n");
            Receive(stream, commands, "200 PRET accepted\r\n");
            SendDownload(
                stream, dataListener, commands, "prepared", "226 Complete\r\n");
        });
        FtpClient client = ConnectAuthenticated(controlListener, true);
        using MemoryStream content = new MemoryStream();
        FtpTransferRequest request = CreateStreamRequest(
            "/folder/file name.bin", content);

        FtpTransferResult result = client.Transfer(request);
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands, Is.EqualTo(new[]
            {
                "USER alice", "PASS secret", "TYPE I",
                "PRET RETR /folder/file name.bin", "EPSV",
                "RETR /folder/file name.bin",
            }));
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Commands.Select(item => item.Command.Kind),
                Is.EqualTo(new[]
                {
                    FtpCommandKind.Type,
                    FtpCommandKind.Pret,
                    FtpCommandKind.Retr,
                }));
            Assert.That(Encoding.ASCII.GetString(content.ToArray()),
                Is.EqualTo("prepared"));
        });

        client.AbortConnection();
    }

    [Test]
    public void RETR_downloads_to_a_caller_stream_and_records_both_replies()
    {
        using TcpListener controlListener = StartListener();
        using TcpListener dataListener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = controlListener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            Receive(stream, commands, "200 Structure selected\r\n");
            Receive(stream, commands, "200 Mode selected\r\n");
            Receive(stream, commands, "200 Type selected\r\n");
            SendDownload(
                stream, dataListener, commands, "hello", "226 Transfer complete\r\n");
        });
        FtpClient client = ConnectAuthenticated(controlListener);
        client.State.Session.State.FileStructure = FtpFileStructure.Record;
        client.State.Session.State.TransferMode = FtpTransferMode.Block;
        using MemoryStream content = new MemoryStream();
        FtpTransferRequest request = CreateStreamRequest("/hello.txt", content);
        request.HasExpectedSize = true;
        request.ExpectedSize = 5;

        FtpTransferResult result = client.Transfer(request);
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands, Is.EqualTo(new[]
            {
                "USER alice", "PASS secret", "STRU F", "MODE S", "TYPE I", "EPSV",
                "RETR /hello.txt",
            }));
            Assert.That(Encoding.ASCII.GetString(content.ToArray()), Is.EqualTo("hello"));
            Assert.That(content.CanWrite, Is.True);
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Commands, Has.Length.EqualTo(4));
            Assert.That(result.Commands[3].PreliminaryReply!.Code, Is.EqualTo(150));
            Assert.That(result.Commands[3].FinalReply!.Code, Is.EqualTo(226));
            Assert.That(result.HasActualSize, Is.True);
            Assert.That(result.ActualSize, Is.EqualTo(5));
            Assert.That(result.Progress.Completed, Is.True);
            Assert.That(result.Progress.ExpectedBytes, Is.EqualTo(5));
        });

        client.AbortConnection();
    }

    [Test]
    public void RETR_overwrites_an_existing_local_file()
    {
        string directory = CreateTemporaryDirectory();
        string path = Path.Combine(directory, "download.bin");
        File.WriteAllText(path, "old content");
        using TcpListener controlListener = StartListener();
        using TcpListener dataListener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = controlListener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            Receive(stream, commands, "200 Type selected\r\n");
            SendDownload(
                stream, dataListener, commands, "new", "226 Transfer complete\r\n");
        });
        FtpClient client = ConnectAuthenticated(controlListener);
        FtpTransferRequest request = CreateFileRequest("/remote.bin", path);
        request.Options.CollisionPolicy = FtpCollisionPolicy.Overwrite;

        FtpTransferResult result = client.Transfer(request);
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(File.ReadAllText(path), Is.EqualTo("new"));
            Assert.That(result.Progress.Path, Is.EqualTo(path));
        });

        client.AbortConnection();
        Directory.Delete(directory, true);
    }

    [Test]
    public void Skip_and_fail_collisions_do_not_contact_the_server_or_change_the_file()
    {
        string directory = CreateTemporaryDirectory();
        string path = Path.Combine(directory, "existing.bin");
        File.WriteAllText(path, "retained");
        using TcpListener listener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = listener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
        });
        FtpClient client = ConnectAuthenticated(listener);
        server.GetAwaiter().GetResult();
        FtpTransferRequest skipped = CreateFileRequest("/remote.bin", path);
        skipped.Options.CollisionPolicy = FtpCollisionPolicy.Skip;
        FtpTransferRequest failed = CreateFileRequest("/remote.bin", path);
        failed.Options.CollisionPolicy = FtpCollisionPolicy.Fail;

        FtpTransferResult skipResult = client.Transfer(skipped);
        FtpTransferResult failResult = client.Transfer(failed);

        Assert.Multiple(() =>
        {
            Assert.That(skipResult.Succeeded, Is.True);
            Assert.That(skipResult.Commands, Is.Empty);
            Assert.That(failResult.Succeeded, Is.False);
            Assert.That(failResult.Commands, Is.Empty);
            Assert.That(failResult.FailureMessage, Does.Contain("already exists"));
            Assert.That(File.ReadAllText(path), Is.EqualTo("retained"));
            Assert.That(commands, Is.EqualTo(new[] { "USER alice", "PASS secret" }));
        });

        client.AbortConnection();
        Directory.Delete(directory, true);
    }

    [Test]
    public void Failed_final_reply_applies_the_partial_file_cleanup_policy()
    {
        string directory = CreateTemporaryDirectory();
        string removedPath = Path.Combine(directory, "removed.bin");
        string retainedPath = Path.Combine(directory, "retained.bin");
        using TcpListener controlListener = StartListener();
        using TcpListener firstDataListener = StartListener();
        using TcpListener secondDataListener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = controlListener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            Receive(stream, commands, "200 Type selected\r\n");
            SendDownload(
                stream, firstDataListener, commands, "partial-one",
                "426 Transfer aborted\r\n");
            SendDownload(
                stream, secondDataListener, commands, "partial-two",
                "426 Transfer aborted\r\n");
        });
        FtpClient client = ConnectAuthenticated(controlListener);
        FtpTransferRequest removed = CreateFileRequest("/one.bin", removedPath);
        removed.Options.CollisionPolicy = FtpCollisionPolicy.Overwrite;
        FtpTransferRequest retained = CreateFileRequest("/two.bin", retainedPath);
        retained.Options.CollisionPolicy = FtpCollisionPolicy.Overwrite;
        retained.Options.CleanupPartialOnFailure = false;

        FtpTransferResult removedResult = client.Transfer(removed);
        FtpTransferResult retainedResult = client.Transfer(retained);
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(removedResult.Succeeded, Is.False);
            Assert.That(removedResult.Commands[^1].FinalReply!.Code, Is.EqualTo(426));
            Assert.That(File.Exists(removedPath), Is.False);
            Assert.That(retainedResult.Succeeded, Is.False);
            Assert.That(File.ReadAllText(retainedPath), Is.EqualTo("partial-two"));
        });

        client.AbortConnection();
        Directory.Delete(directory, true);
    }

    [Test]
    public void RETR_rejection_before_a_preliminary_reply_removes_the_empty_partial_file()
    {
        string directory = CreateTemporaryDirectory();
        string path = Path.Combine(directory, "rejected.bin");
        using TcpListener controlListener = StartListener();
        using TcpListener dataListener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = controlListener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            Receive(stream, commands, "200 Type selected\r\n");
            commands.Add(ReadCommand(stream));
            WriteText(stream, "229 Entering Extended Passive Mode (|||" +
                GetPort(dataListener).ToString() + "|)\r\n");
            using TcpClient data = dataListener.AcceptTcpClient();
            commands.Add(ReadCommand(stream));
            WriteText(stream, "550 Remote file unavailable\r\n");
        });
        FtpClient client = ConnectAuthenticated(controlListener);
        FtpTransferRequest request = CreateFileRequest("/missing.bin", path);
        request.Options.CollisionPolicy = FtpCollisionPolicy.Overwrite;

        FtpTransferResult result = client.Transfer(request);
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands, Is.EqualTo(new[]
            {
                "USER alice", "PASS secret", "TYPE I", "EPSV",
                "RETR /missing.bin",
            }));
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.FailureMessage, Does.Contain("RETR failed with reply 550"));
            Assert.That(result.Commands[^1].PreliminaryReply, Is.Null);
            Assert.That(result.Commands[^1].FinalReply!.Code, Is.EqualTo(550));
            Assert.That(File.Exists(path), Is.False);
            Assert.That(client.State.DataStrategy, Is.Null);
            Assert.That(client.GetSessionState(), Is.EqualTo(FtpSessionState.Authenticated));
        });

        client.AbortConnection();
        Directory.Delete(directory, true);
    }

    [Test]
    public void Append_cleanup_restores_length_and_unique_selects_a_new_local_name()
    {
        string directory = CreateTemporaryDirectory();
        string path = Path.Combine(directory, "file.txt");
        File.WriteAllText(path, "original");
        using TcpListener controlListener = StartListener();
        using TcpListener firstDataListener = StartListener();
        using TcpListener secondDataListener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = controlListener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            Receive(stream, commands, "200 Type selected\r\n");
            SendDownload(
                stream, firstDataListener, commands, "partial",
                "426 Transfer aborted\r\n");
            SendDownload(
                stream, secondDataListener, commands, "unique",
                "226 Transfer complete\r\n");
        });
        FtpClient client = ConnectAuthenticated(controlListener);
        FtpTransferRequest append = CreateFileRequest("/append.txt", path);
        append.Options.CollisionPolicy = FtpCollisionPolicy.Append;
        FtpTransferRequest unique = CreateFileRequest("/unique.txt", path);
        unique.Options.CollisionPolicy = FtpCollisionPolicy.Unique;

        FtpTransferResult appendResult = client.Transfer(append);
        FtpTransferResult uniqueResult = client.Transfer(unique);
        server.GetAwaiter().GetResult();
        string uniquePath = Path.Combine(directory, "file (1).txt");

        Assert.Multiple(() =>
        {
            Assert.That(appendResult.Succeeded, Is.False);
            Assert.That(File.ReadAllText(path), Is.EqualTo("original"));
            Assert.That(uniqueResult.Succeeded, Is.True);
            Assert.That(uniqueResult.Progress.Path, Is.EqualTo(uniquePath));
            Assert.That(File.ReadAllText(uniquePath), Is.EqualTo("unique"));
        });

        client.AbortConnection();
        Directory.Delete(directory, true);
    }

    [Test]
    public void REST_STREAM_resumes_after_stable_SIZE_MDTM_and_unique_validation()
    {
        using TcpListener controlListener = StartListener();
        using TcpListener dataListener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = controlListener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            Receive(stream, commands, "200 Type selected\r\n");
            ReceiveStableResumeMetadata(stream, commands, "/file.bin", "remote-1");
            SendResumedDownload(
                stream, dataListener, commands, 6, "world", "226 Transfer complete\r\n");
        });
        FtpClient client = ConnectAuthenticated(controlListener);
        using MemoryStream content = new MemoryStream();
        WriteText(content, "hello ");
        content.Position = 0;
        FtpTransferRequest request = CreateStreamRequest("/file.bin", content);
        request.HasRestartOffset = true;
        request.RestartOffset = 6;
        request.HasExpectedSize = true;
        request.ExpectedSize = 11;
        request.Options.ResumePolicy = FtpResumePolicy.Required;

        FtpTransferResult result = client.Transfer(request);
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands, Is.EqualTo(new[]
            {
                "USER alice", "PASS secret", "TYPE I",
                "MLST /file.bin", "SIZE /file.bin", "MDTM /file.bin",
                "MLST /file.bin", "EPSV", "REST 6", "RETR /file.bin",
            }));
            Assert.That(Encoding.ASCII.GetString(content.ToArray()),
                Is.EqualTo("hello world"));
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.RestartMarker, Is.EqualTo("6"));
            Assert.That(result.HasActualSize, Is.True);
            Assert.That(result.ActualSize, Is.EqualTo(11));
            Assert.That(result.Progress.LogicalBytes, Is.EqualTo(11));
            Assert.That(result.Progress.WireBytes, Is.EqualTo(5));
        });

        client.AbortConnection();
    }

    [TestCase(true)]
    [TestCase(false)]
    public void Required_resume_rejects_SIZE_or_MDTM_changes_before_REST(
        bool sizeMismatch)
    {
        using TcpListener listener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = listener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            Receive(stream, commands, "200 Type selected\r\n");
            ReceiveMlst(stream, commands, "/file.bin", "remote-1", true);
            Receive(stream, commands, sizeMismatch ? "213 12\r\n" : "213 11\r\n");
            if (!sizeMismatch)
            {
                Receive(stream, commands, "213 20240830120001\r\n");
            }
        });
        FtpClient client = ConnectAuthenticated(listener);
        using MemoryStream content = new MemoryStream();
        WriteText(content, "hello ");
        content.Position = 0;
        FtpTransferRequest request = CreateStreamRequest("/file.bin", content);
        request.HasRestartOffset = true;
        request.RestartOffset = 6;
        request.Options.ResumePolicy = FtpResumePolicy.Required;

        FtpTransferResult result = client.Transfer(request);
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.FailureMessage, Does.Contain(
                sizeMismatch ? "size is inconsistent" : "modification time changed"));
            Assert.That(commands.Skip(2), Is.EqualTo(sizeMismatch ? new[]
            {
                "TYPE I", "MLST /file.bin", "SIZE /file.bin",
            } : new[]
            {
                "TYPE I", "MLST /file.bin", "SIZE /file.bin", "MDTM /file.bin",
            }));
            Assert.That(commands.Any(command => command.StartsWith("REST")), Is.False);
            Assert.That(Encoding.ASCII.GetString(content.ToArray()), Is.EqualTo("hello "));
        });

        client.AbortConnection();
    }

    [Test]
    public void Zero_offset_download_validates_metadata_and_sends_REST_zero()
    {
        using TcpListener controlListener = StartListener();
        using TcpListener dataListener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = controlListener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            Receive(stream, commands, "200 Type selected\r\n");
            Receive(
                stream,
                commands,
                "250-Listing\r\n Type=file;Size=4;Modify=20240830120000;" +
                    "Unique=zero; /zero.bin\r\n250 End\r\n");
            Receive(stream, commands, "213 4\r\n");
            Receive(stream, commands, "213 20240830120000\r\n");
            Receive(
                stream,
                commands,
                "250-Listing\r\n Type=file;Size=4;Modify=20240830120000;" +
                    "Unique=zero; /zero.bin\r\n250 End\r\n");
            SendResumedDownload(
                stream, dataListener, commands, 0, "zero", "226 Transfer complete\r\n");
        });
        FtpClient client = ConnectAuthenticated(controlListener);
        using MemoryStream content = new MemoryStream();
        FtpTransferRequest request = CreateStreamRequest("/zero.bin", content);
        request.HasRestartOffset = true;
        request.RestartOffset = 0;
        request.HasExpectedSize = true;
        request.ExpectedSize = 4;
        request.Options.ResumePolicy = FtpResumePolicy.Required;

        FtpTransferResult result = client.Transfer(request);
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands.Skip(2), Is.EqualTo(new[]
            {
                "TYPE I", "MLST /zero.bin", "SIZE /zero.bin", "MDTM /zero.bin",
                "MLST /zero.bin", "EPSV", "REST 0", "RETR /zero.bin",
            }));
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.RestartMarker, Is.EqualTo("0"));
            Assert.That(result.ActualSize, Is.EqualTo(4));
            Assert.That(Encoding.ASCII.GetString(content.ToArray()), Is.EqualTo("zero"));
        });

        client.AbortConnection();
    }

    [Test]
    public void Large_offset_download_uses_a_64_bit_REST_value_and_sparse_file()
    {
        const long restartOffset = 3000000000;
        const long remoteSize = restartOffset + 1;
        string directory = CreateTemporaryDirectory();
        string path = Path.Combine(directory, "large.bin");
        using (FileStream partial = new FileStream(
            path, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            partial.SetLength(restartOffset);
        }
        using TcpListener controlListener = StartListener();
        using TcpListener dataListener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = controlListener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            Receive(stream, commands, "200 Type selected\r\n");
            string metadata =
                "250-Listing\r\n Type=file;Size=" + remoteSize.ToString() +
                ";Modify=20240830120000;Unique=large; /large.bin\r\n250 End\r\n";
            Receive(stream, commands, metadata);
            Receive(stream, commands, "213 " + remoteSize.ToString() + "\r\n");
            Receive(stream, commands, "213 20240830120000\r\n");
            Receive(stream, commands, metadata);
            SendResumedDownload(
                stream, dataListener, commands, restartOffset, "Z",
                "226 Transfer complete\r\n");
        });
        FtpClient client = ConnectAuthenticated(controlListener);
        FtpTransferRequest request = CreateFileRequest("/large.bin", path);
        request.Options.CollisionPolicy = FtpCollisionPolicy.Resume;
        request.Options.ResumePolicy = FtpResumePolicy.Required;
        request.HasRestartOffset = true;
        request.RestartOffset = restartOffset;
        request.HasExpectedSize = true;
        request.ExpectedSize = remoteSize;

        FtpTransferResult result = client.Transfer(request);
        server.GetAwaiter().GetResult();
        using FileStream completed = new FileStream(
            path, FileMode.Open, FileAccess.Read, FileShare.Read);
        completed.Seek(restartOffset, SeekOrigin.Begin);

        Assert.Multiple(() =>
        {
            Assert.That(commands, Does.Contain("REST 3000000000"));
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.RestartMarker, Is.EqualTo("3000000000"));
            Assert.That(result.ActualSize, Is.EqualTo(remoteSize));
            Assert.That(result.Progress.WireBytes, Is.EqualTo(1));
            Assert.That(completed.Length, Is.EqualTo(remoteSize));
            Assert.That(completed.ReadByte(), Is.EqualTo((int)'Z'));
        });

        client.AbortConnection();
        completed.Dispose();
        Directory.Delete(directory, true);
    }

    [Test]
    public void Required_resume_rejects_local_length_mismatch_and_nonseekable_stream()
    {
        string directory = CreateTemporaryDirectory();
        string path = Path.Combine(directory, "partial.bin");
        File.WriteAllText(path, "three");
        using TcpListener listener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = listener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
        });
        FtpClient client = ConnectAuthenticated(listener);
        server.GetAwaiter().GetResult();
        FtpTransferRequest mismatch = CreateFileRequest("/file.bin", path);
        mismatch.HasRestartOffset = true;
        mismatch.RestartOffset = 3;
        mismatch.Options.ResumePolicy = FtpResumePolicy.Required;
        using NonSeekableWriteStream nonSeekable = new NonSeekableWriteStream();
        FtpTransferRequest streamRequest =
            CreateStreamRequest("/file.bin", nonSeekable);
        streamRequest.Options.ResumePolicy = FtpResumePolicy.Required;

        Assert.Multiple(() =>
        {
            Assert.That(
                () => client.Transfer(mismatch),
                Throws.TypeOf<FtpConfigurationException>()
                    .With.Message.Contains("length"));
            Assert.That(
                () => client.Transfer(streamRequest),
                Throws.TypeOf<FtpConfigurationException>()
                    .With.Message.Contains("seekable"));
            Assert.That(commands, Is.EqualTo(new[] { "USER alice", "PASS secret" }));
            Assert.That(File.ReadAllText(path), Is.EqualTo("three"));
        });

        client.AbortConnection();
        Directory.Delete(directory, true);
    }

    [Test]
    public void Required_resume_refuses_a_remote_unique_identity_change_before_REST()
    {
        string directory = CreateTemporaryDirectory();
        string path = Path.Combine(directory, "partial.bin");
        File.WriteAllText(path, "hello ");
        using TcpListener listener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = listener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            Receive(stream, commands, "200 Type selected\r\n");
            ReceiveMlst(stream, commands, "/file.bin", "remote-1", true);
            Receive(stream, commands, "213 11\r\n");
            Receive(stream, commands, "213 20240830120000\r\n");
            ReceiveMlst(stream, commands, "/file.bin", "remote-2", true);
        });
        FtpClient client = ConnectAuthenticated(listener);
        FtpTransferRequest request = CreateFileRequest("/file.bin", path);
        request.Options.CollisionPolicy = FtpCollisionPolicy.Resume;
        request.Options.ResumePolicy = FtpResumePolicy.Required;

        FtpTransferResult result = client.Transfer(request);
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.FailureMessage, Does.Contain("identity changed"));
            Assert.That(commands, Is.EqualTo(new[]
            {
                "USER alice", "PASS secret", "TYPE I",
                "MLST /file.bin", "SIZE /file.bin", "MDTM /file.bin",
                "MLST /file.bin",
            }));
            Assert.That(File.ReadAllText(path), Is.EqualTo("hello "));
        });

        client.AbortConnection();
        Directory.Delete(directory, true);
    }

    [Test]
    public void IfSafe_resume_falls_back_to_a_full_download_when_unique_is_unavailable()
    {
        string directory = CreateTemporaryDirectory();
        string path = Path.Combine(directory, "partial.bin");
        File.WriteAllText(path, "partial");
        using TcpListener controlListener = StartListener();
        using TcpListener dataListener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = controlListener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            Receive(stream, commands, "200 Type selected\r\n");
            ReceiveMlst(stream, commands, "/file.bin", "", false);
            SendDownload(
                stream, dataListener, commands, "complete", "226 Transfer complete\r\n");
        });
        FtpClient client = ConnectAuthenticated(controlListener);
        FtpTransferRequest request = CreateFileRequest("/file.bin", path);
        request.Options.ResumePolicy = FtpResumePolicy.IfSafe;

        FtpTransferResult result = client.Transfer(request);
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.RestartMarker, Is.Null);
            Assert.That(File.ReadAllText(path), Is.EqualTo("complete"));
            Assert.That(commands, Is.EqualTo(new[]
            {
                "USER alice", "PASS secret", "TYPE I", "MLST /file.bin",
                "EPSV", "RETR /file.bin",
            }));
        });

        client.AbortConnection();
        Directory.Delete(directory, true);
    }

    private static FtpTransferRequest CreateStreamRequest(
        string remotePath,
        Stream content)
    {
        FtpTransferRequest request = new FtpTransferRequest();
        request.RemotePath = remotePath;
        request.Content = content;
        request.Options.VerificationMode = FtpVerificationMode.None;
        return request;
    }

    private static FtpTransferRequest CreateFileRequest(
        string remotePath,
        string localPath)
    {
        FtpTransferRequest request = new FtpTransferRequest();
        request.RemotePath = remotePath;
        request.LocalPath = localPath;
        request.Options.VerificationMode = FtpVerificationMode.None;
        return request;
    }

    private static string CreateTemporaryDirectory()
    {
        string path = Path.Combine(
            Path.GetTempPath(), "ftp-download-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static void SendDownload(
        NetworkStream controlStream,
        TcpListener dataListener,
        List<string> commands,
        string content,
        string finalReply)
    {
        commands.Add(ReadCommand(controlStream));
        WriteText(controlStream, "229 Entering Extended Passive Mode (|||" +
            GetPort(dataListener).ToString() + "|)\r\n");
        using TcpClient data = dataListener.AcceptTcpClient();
        commands.Add(ReadCommand(controlStream));
        WriteText(controlStream, "150 Opening data\r\n");
        using (NetworkStream dataStream = data.GetStream())
        {
            WriteText(dataStream, content);
        }
        WriteText(controlStream, finalReply);
    }

    private static void SendResumedDownload(
        NetworkStream controlStream,
        TcpListener dataListener,
        List<string> commands,
        long restartOffset,
        string content,
        string finalReply)
    {
        commands.Add(ReadCommand(controlStream));
        WriteText(controlStream, "229 Entering Extended Passive Mode (|||" +
            GetPort(dataListener).ToString() + "|)\r\n");
        using TcpClient data = dataListener.AcceptTcpClient();
        Receive(
            controlStream, commands,
            "350 Restarting at " + restartOffset.ToString() + "\r\n");
        commands.Add(ReadCommand(controlStream));
        WriteText(controlStream, "150 Opening data\r\n");
        using (NetworkStream dataStream = data.GetStream())
        {
            WriteText(dataStream, content);
        }
        WriteText(controlStream, finalReply);
    }

    private static void ReceiveStableResumeMetadata(
        NetworkStream stream,
        List<string> commands,
        string path,
        string unique)
    {
        ReceiveMlst(stream, commands, path, unique, true);
        Receive(stream, commands, "213 11\r\n");
        Receive(stream, commands, "213 20240830120000\r\n");
        ReceiveMlst(stream, commands, path, unique, true);
    }

    private static void ReceiveMlst(
        NetworkStream stream,
        List<string> commands,
        string path,
        string unique,
        bool includeUnique)
    {
        string uniqueFact = includeUnique ? "Unique=" + unique + ";" : "";
        Receive(
            stream,
            commands,
            "250-Listing\r\n Type=file;Size=11;Modify=20240830120000;" +
                uniqueFact + " " + path + "\r\n250 End\r\n");
    }

    private static void WriteText(Stream stream, string text)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(text);
        stream.Write(bytes, 0, bytes.Length);
        stream.Flush();
    }

    private static TcpListener StartListener()
    {
        TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return listener;
    }

    private static int GetPort(TcpListener listener)
    {
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private static FtpClient ConnectAuthenticated(
        TcpListener listener,
        bool enablePret = false)
    {
        FtpClientOptions options = new FtpClientOptions();
        options.Endpoint.Host = IPAddress.Loopback.ToString();
        options.Endpoint.Port = GetPort(listener);
        options.Endpoint.AddressFamily = FtpAddressFamily.IPv4;
        options.SecurityMode = FtpSecurityMode.Plain;
        options.AllowPlaintextCredentials = true;
        options.NegotiateFeatures = false;
        options.EnablePret = enablePret;
        options.DataConnection.Mode = FtpDataConnectionMode.Passive;
        FtpClient client = new FtpClient(null);
        client.Connect(options);
        FtpCredentials credentials = new FtpCredentials();
        credentials.UserName = "alice";
        credentials.Password = "secret";
        FtpAuthenticationResult authentication = client.AuthenticateUser(credentials);
        Assert.That(authentication.Status, Is.EqualTo(FtpAuthenticationStatus.Success));
        return client;
    }

    private static void Authenticate(NetworkStream stream, List<string> commands)
    {
        WriteText(stream, "220 Ready\r\n");
        Receive(stream, commands, "331 Password required\r\n");
        Receive(stream, commands, "230 Logged in\r\n");
    }

    private static void Receive(
        NetworkStream stream,
        List<string> commands,
        string reply)
    {
        commands.Add(ReadCommand(stream));
        WriteText(stream, reply);
    }

    private static string ReadCommand(NetworkStream stream)
    {
        List<byte> bytes = new List<byte>();
        while (true)
        {
            int value = stream.ReadByte();
            if (value < 0)
            {
                throw new EndOfStreamException();
            }
            if (value == '\n')
            {
                break;
            }
            if (value != '\r')
            {
                bytes.Add((byte)value);
            }
        }
        return Encoding.ASCII.GetString(bytes.ToArray());
    }

    private static void WriteText(NetworkStream stream, string text)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(text);
        stream.Write(bytes, 0, bytes.Length);
        stream.Flush();
    }

    private sealed class NonSeekableWriteStream : Stream
    {
        private readonly MemoryStream inner = new MemoryStream();

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
            inner.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            inner.Write(buffer, offset, count);
        }
    }
}
