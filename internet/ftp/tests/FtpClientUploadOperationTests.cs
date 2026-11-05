using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ftp.Tests;

public class FtpClientUploadOperationTests
{
    [Test]
    public void PRET_announces_the_exact_STOR_before_passive_setup()
    {
        using TcpListener controlListener = StartListener();
        using TcpListener dataListener = StartListener();
        List<string> commands = new List<string>();
        List<string> uploads = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = controlListener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            Receive(stream, commands, "200 Type selected\r\n");
            Receive(stream, commands, "200 PRET accepted\r\n");
            uploads.Add(ReceiveUpload(
                stream, dataListener, commands, "150 Opening data\r\n",
                "226 Complete\r\n"));
        });
        FtpClient client = ConnectAuthenticated(controlListener, true);
        using MemoryStream content = new MemoryStream(
            Encoding.ASCII.GetBytes("prepared"));
        FtpTransferRequest request = CreateStreamRequest(
            "/incoming/file name.bin", content, FtpCollisionPolicy.Overwrite);

        FtpTransferResult result = client.Transfer(request);
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands, Is.EqualTo(new[]
            {
                "USER alice", "PASS secret", "TYPE I",
                "PRET STOR /incoming/file name.bin", "EPSV",
                "STOR /incoming/file name.bin",
            }));
            Assert.That(uploads, Is.EqualTo(new[] { "prepared" }));
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Commands.Select(item => item.Command.Kind),
                Is.EqualTo(new[]
                {
                    FtpCommandKind.Type,
                    FtpCommandKind.Pret,
                    FtpCommandKind.Stor,
                }));
        });

        client.AbortConnection();
    }

    [Test]
    public void STOR_uploads_a_caller_stream_and_records_transfer_replies()
    {
        using TcpListener controlListener = StartListener();
        using TcpListener dataListener = StartListener();
        List<string> commands = new List<string>();
        List<string> uploads = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = controlListener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            Receive(stream, commands, "200 Type selected\r\n");
            uploads.Add(ReceiveUpload(
                stream, dataListener, commands, "150 Opening data\r\n",
                "226 Transfer complete\r\n"));
        });
        FtpClient client = ConnectAuthenticated(controlListener);
        using MemoryStream content = new MemoryStream(
            Encoding.ASCII.GetBytes("uploaded"));
        FtpTransferRequest request = CreateStreamRequest(
            "/incoming/file.txt", content, FtpCollisionPolicy.Overwrite);
        request.HasExpectedSize = true;
        request.ExpectedSize = 8;

        FtpTransferResult result = client.Transfer(request);
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands, Is.EqualTo(new[]
            {
                "USER alice", "PASS secret", "TYPE I", "EPSV",
                "STOR /incoming/file.txt",
            }));
            Assert.That(uploads, Is.EqualTo(new[] { "uploaded" }));
            Assert.That(content.CanRead, Is.True);
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Commands, Has.Length.EqualTo(2));
            Assert.That(result.Commands[1].PreliminaryReply!.Code, Is.EqualTo(150));
            Assert.That(result.Commands[1].FinalReply!.Code, Is.EqualTo(226));
            Assert.That(result.ActualSize, Is.EqualTo(8));
            Assert.That(result.Progress.Completed, Is.True);
        });

        client.AbortConnection();
    }

    [Test]
    public void STOR_records_initial_and_final_rejections_without_consuming_early_content()
    {
        using TcpListener controlListener = StartListener();
        using TcpListener rejectedDataListener = StartListener();
        using TcpListener failedDataListener = StartListener();
        List<string> commands = new List<string>();
        List<string> uploads = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = controlListener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            Receive(stream, commands, "200 Type selected\r\n");

            commands.Add(ReadCommand(stream));
            WriteText(stream, "229 Entering Extended Passive Mode (|||" +
                GetPort(rejectedDataListener).ToString() + "|)\r\n");
            using (TcpClient rejectedData = rejectedDataListener.AcceptTcpClient())
            {
                commands.Add(ReadCommand(stream));
                WriteText(stream, "550 STOR rejected\r\n");
            }

            uploads.Add(ReceiveUpload(
                stream, failedDataListener, commands, "150 Opening data\r\n",
                "426 Upload aborted\r\n"));
        });
        FtpClient client = ConnectAuthenticated(controlListener);
        using MemoryStream rejectedContent = new MemoryStream(
            Encoding.ASCII.GetBytes("not-consumed"));
        using MemoryStream failedContent = new MemoryStream(
            Encoding.ASCII.GetBytes("partial-remote"));
        FtpTransferRequest rejected = CreateStreamRequest(
            "/rejected.bin", rejectedContent, FtpCollisionPolicy.Overwrite);
        FtpTransferRequest failed = CreateStreamRequest(
            "/failed.bin", failedContent, FtpCollisionPolicy.Overwrite);

        FtpTransferResult rejectedResult = client.Transfer(rejected);
        FtpTransferResult failedResult = client.Transfer(failed);
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands, Is.EqualTo(new[]
            {
                "USER alice", "PASS secret", "TYPE I",
                "EPSV", "STOR /rejected.bin",
                "EPSV", "STOR /failed.bin",
            }));
            Assert.That(rejectedResult.Succeeded, Is.False);
            Assert.That(rejectedResult.Commands[^1].PreliminaryReply, Is.Null);
            Assert.That(rejectedResult.Commands[^1].FinalReply!.Code, Is.EqualTo(550));
            Assert.That(rejectedContent.Position, Is.Zero);
            Assert.That(failedResult.Succeeded, Is.False);
            Assert.That(failedResult.Commands[^1].PreliminaryReply!.Code, Is.EqualTo(150));
            Assert.That(failedResult.Commands[^1].FinalReply!.Code, Is.EqualTo(426));
            Assert.That(uploads, Is.EqualTo(new[] { "partial-remote" }));
            Assert.That(client.State.DataStrategy, Is.Null);
            Assert.That(client.GetSessionState(), Is.EqualTo(FtpSessionState.Authenticated));
        });

        client.AbortConnection();
    }

    [Test]
    public void Append_uses_APPE_and_unique_uses_STOU_with_returned_name()
    {
        using TcpListener controlListener = StartListener();
        using TcpListener appendDataListener = StartListener();
        using TcpListener uniqueDataListener = StartListener();
        List<string> commands = new List<string>();
        List<string> uploads = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = controlListener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            Receive(stream, commands, "200 Type selected\r\n");
            uploads.Add(ReceiveUpload(
                stream, appendDataListener, commands, "150 Opening append\r\n",
                "226 Append complete\r\n"));
            uploads.Add(ReceiveUpload(
                stream, uniqueDataListener, commands,
                "150 FILE: generated-42.bin\r\n",
                "226 Unique transfer complete\r\n"));
        });
        FtpClient client = ConnectAuthenticated(controlListener);
        using MemoryStream appendContent = new MemoryStream(
            Encoding.ASCII.GetBytes("tail"));
        using MemoryStream uniqueContent = new MemoryStream(
            Encoding.ASCII.GetBytes("unique"));
        FtpTransferRequest append = CreateStreamRequest(
            "/log.txt", appendContent, FtpCollisionPolicy.Append);
        FtpTransferRequest unique = CreateStreamRequest(
            "/hint.bin", uniqueContent, FtpCollisionPolicy.Unique);

        FtpTransferResult appendResult = client.Transfer(append);
        FtpTransferResult uniqueResult = client.Transfer(unique);
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands, Is.EqualTo(new[]
            {
                "USER alice", "PASS secret", "TYPE I",
                "EPSV", "APPE /log.txt", "EPSV", "STOU",
            }));
            Assert.That(uploads, Is.EqualTo(new[] { "tail", "unique" }));
            Assert.That(appendResult.Succeeded, Is.True);
            Assert.That(uniqueResult.Succeeded, Is.True);
            Assert.That(uniqueResult.UniqueRemoteName, Is.EqualTo("generated-42.bin"));
            Assert.That(uniqueResult.Commands[^1].Command.Kind,
                Is.EqualTo(FtpCommandKind.Stou));
        });

        client.AbortConnection();
    }

    [Test]
    public void STOU_extracts_the_unique_remote_name_from_the_final_reply()
    {
        using TcpListener controlListener = StartListener();
        using TcpListener dataListener = StartListener();
        List<string> commands = new List<string>();
        List<string> uploads = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = controlListener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            Receive(stream, commands, "200 Type selected\r\n");
            uploads.Add(ReceiveUpload(
                stream, dataListener, commands, "150 \r\n",
                "226 FILE: generated-final.bin\r\n"));
        });
        FtpClient client = ConnectAuthenticated(controlListener);
        using MemoryStream content = new MemoryStream(
            Encoding.ASCII.GetBytes("unique-final"));
        FtpTransferRequest request = CreateStreamRequest(
            "/ignored-hint.bin", content, FtpCollisionPolicy.Unique);

        FtpTransferResult result = client.Transfer(request);
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands, Is.EqualTo(new[]
            {
                "USER alice", "PASS secret", "TYPE I", "EPSV", "STOU",
            }));
            Assert.That(uploads, Is.EqualTo(new[] { "unique-final" }));
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.UniqueRemoteName, Is.EqualTo("generated-final.bin"));
            Assert.That(result.Commands[^1].PreliminaryReply!.Text, Is.Empty);
            Assert.That(result.Commands[^1].FinalReply!.Code, Is.EqualTo(226));
        });

        client.AbortConnection();
    }

    [Test]
    public void File_upload_creates_remote_parents_before_STOR()
    {
        string directory = CreateTemporaryDirectory();
        string localPath = Path.Combine(directory, "source.bin");
        File.WriteAllText(localPath, "file-source");
        using TcpListener controlListener = StartListener();
        using TcpListener dataListener = StartListener();
        List<string> commands = new List<string>();
        List<string> uploads = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = controlListener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            Receive(stream, commands, "257 \"/home\" is current\r\n");
            Receive(stream, commands, "550 Missing\r\n");
            Receive(stream, commands, "257 \"/incoming\" created\r\n");
            Receive(stream, commands, "550 Missing\r\n");
            Receive(stream, commands, "257 \"/incoming/2026\" created\r\n");
            Receive(stream, commands, "200 Type selected\r\n");
            uploads.Add(ReceiveUpload(
                stream, dataListener, commands, "150 Opening data\r\n",
                "226 Transfer complete\r\n"));
        });
        FtpClient client = ConnectAuthenticated(controlListener);
        FtpTransferRequest request = new FtpTransferRequest();
        request.Direction = FtpTransferDirection.Upload;
        request.RemotePath = "/incoming/2026/file.bin";
        request.LocalPath = localPath;
        request.Options.CollisionPolicy = FtpCollisionPolicy.Overwrite;
        request.Options.CreateParentDirectories = true;
        request.Options.VerificationMode = FtpVerificationMode.None;

        FtpTransferResult result = client.Transfer(request);
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands.Skip(2), Is.EqualTo(new[]
            {
                "PWD", "CWD /incoming", "MKD /incoming",
                "CWD /incoming/2026", "MKD /incoming/2026",
                "TYPE I", "EPSV", "STOR /incoming/2026/file.bin",
            }));
            Assert.That(uploads, Is.EqualTo(new[] { "file-source" }));
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Commands.Select(command => command.Command.Kind),
                Is.EqualTo(new[]
                {
                    FtpCommandKind.Pwd, FtpCommandKind.Cwd, FtpCommandKind.Mkd,
                    FtpCommandKind.Cwd, FtpCommandKind.Mkd,
                    FtpCommandKind.Type, FtpCommandKind.Stor,
                }));
        });

        client.AbortConnection();
        Directory.Delete(directory, true);
    }

    [Test]
    public void Failed_remote_parent_creation_prevents_STOR_and_preserves_the_local_file()
    {
        string directory = CreateTemporaryDirectory();
        string localPath = Path.Combine(directory, "source.bin");
        File.WriteAllText(localPath, "retained-source");
        using TcpListener listener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = listener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            Receive(stream, commands, "257 \"/home\" is current\r\n");
            Receive(stream, commands, "550 Parent missing\r\n");
            Receive(stream, commands, "550 Parent creation denied\r\n");
            Receive(stream, commands, "550 Parent still missing\r\n");
        });
        FtpClient client = ConnectAuthenticated(listener);
        FtpTransferRequest request = new FtpTransferRequest();
        request.Direction = FtpTransferDirection.Upload;
        request.RemotePath = "/blocked/file.bin";
        request.LocalPath = localPath;
        request.Options.CollisionPolicy = FtpCollisionPolicy.Overwrite;
        request.Options.CreateParentDirectories = true;
        request.Options.VerificationMode = FtpVerificationMode.None;

        FtpTransferResult result = client.Transfer(request);
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands.Skip(2), Is.EqualTo(new[]
            {
                "PWD", "CWD /blocked", "MKD /blocked", "CWD /blocked",
            }));
            Assert.That(commands.Any(command => command.StartsWith("STOR")), Is.False);
            Assert.That(commands.Any(command => command == "EPSV"), Is.False);
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Commands[^1].Command.Kind, Is.EqualTo(FtpCommandKind.Cwd));
            Assert.That(result.Commands[^1].FinalReply!.Code, Is.EqualTo(550));
            Assert.That(File.ReadAllText(localPath), Is.EqualTo("retained-source"));
            Assert.That(client.GetSessionState(), Is.EqualTo(FtpSessionState.Authenticated));
        });

        client.AbortConnection();
        Directory.Delete(directory, true);
    }

    [Test]
    public void Skip_and_fail_remote_collisions_never_open_a_data_transfer()
    {
        using TcpListener listener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = listener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            Receive(stream, commands,
                "250-Listing\r\n Type=file;Size=3; /exists.bin\r\n250 End\r\n");
            Receive(stream, commands,
                "250-Listing\r\n Type=file;Size=3; /exists.bin\r\n250 End\r\n");
        });
        FtpClient client = ConnectAuthenticated(listener);
        client.State.Features.Negotiated = true;
        client.State.FeatureState.MlstAdvertised = true;
        using MemoryStream skipContent = new MemoryStream(new byte[] { 1 });
        using MemoryStream failContent = new MemoryStream(new byte[] { 2 });
        FtpTransferRequest skip = CreateStreamRequest(
            "/exists.bin", skipContent, FtpCollisionPolicy.Skip);
        FtpTransferRequest fail = CreateStreamRequest(
            "/exists.bin", failContent, FtpCollisionPolicy.Fail);

        FtpTransferResult skipResult = client.Transfer(skip);
        FtpTransferResult failResult = client.Transfer(fail);
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands, Is.EqualTo(new[]
            {
                "USER alice", "PASS secret", "MLST /exists.bin",
                "MLST /exists.bin",
            }));
            Assert.That(skipResult.Succeeded, Is.True);
            Assert.That(skipResult.Commands, Has.Length.EqualTo(1));
            Assert.That(failResult.Succeeded, Is.False);
            Assert.That(failResult.FailureMessage, Does.Contain("already exists"));
            Assert.That(failResult.Commands, Has.Length.EqualTo(1));
        });

        client.AbortConnection();
    }

    [Test]
    public void REST_STREAM_upload_resume_seeks_locally_and_uses_STOR()
    {
        using TcpListener controlListener = StartListener();
        using TcpListener dataListener = StartListener();
        List<string> commands = new List<string>();
        List<string> uploads = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = controlListener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            Receive(stream, commands, "200 Type selected\r\n");
            Receive(stream, commands, "213 6\r\n");
            uploads.Add(ReceiveResumedUpload(
                stream, dataListener, commands, 6, "150 Opening resumed data\r\n",
                "226 Resume complete\r\n"));
        });
        FtpClient client = ConnectAuthenticated(controlListener);
        using MemoryStream content = new MemoryStream(
            Encoding.ASCII.GetBytes("hello world"));
        content.Position = 2;
        FtpTransferRequest request = CreateStreamRequest(
            "/file.bin", content, FtpCollisionPolicy.Resume);
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
                "USER alice", "PASS secret", "TYPE I", "SIZE /file.bin",
                "EPSV", "REST 6", "STOR /file.bin",
            }));
            Assert.That(uploads, Is.EqualTo(new[] { "world" }));
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.RestartMarker, Is.EqualTo("6"));
            Assert.That(result.ActualSize, Is.EqualTo(11));
            Assert.That(result.Progress.LogicalBytes, Is.EqualTo(11));
            Assert.That(result.Progress.WireBytes, Is.EqualTo(5));
        });

        client.AbortConnection();
    }

    [Test]
    public void Zero_offset_upload_validates_SIZE_without_sending_REST_zero()
    {
        using TcpListener controlListener = StartListener();
        using TcpListener dataListener = StartListener();
        List<string> commands = new List<string>();
        List<string> uploads = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = controlListener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            Receive(stream, commands, "200 Type selected\r\n");
            Receive(stream, commands, "213 0\r\n");
            uploads.Add(ReceiveUpload(
                stream, dataListener, commands, "150 Opening data\r\n",
                "226 Transfer complete\r\n"));
        });
        FtpClient client = ConnectAuthenticated(controlListener);
        using MemoryStream content = new MemoryStream(
            Encoding.ASCII.GetBytes("zero-offset"));
        FtpTransferRequest request = CreateStreamRequest(
            "/zero.bin", content, FtpCollisionPolicy.Resume);
        request.HasRestartOffset = true;
        request.RestartOffset = 0;
        request.HasExpectedSize = true;
        request.ExpectedSize = content.Length;
        request.Options.ResumePolicy = FtpResumePolicy.Required;

        FtpTransferResult result = client.Transfer(request);
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands, Is.EqualTo(new[]
            {
                "USER alice", "PASS secret", "TYPE I", "SIZE /zero.bin",
                "EPSV", "STOR /zero.bin",
            }));
            Assert.That(commands.Any(command => command.StartsWith("REST")), Is.False);
            Assert.That(uploads, Is.EqualTo(new[] { "zero-offset" }));
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.RestartMarker, Is.Null);
            Assert.That(result.ActualSize, Is.EqualTo(content.Length));
        });

        client.AbortConnection();
    }

    [Test]
    public void Large_offset_upload_uses_a_64_bit_REST_value_and_sparse_file()
    {
        const long restartOffset = 3000000000;
        const long sourceSize = restartOffset + 1;
        string directory = CreateTemporaryDirectory();
        string path = Path.Combine(directory, "large.bin");
        using (FileStream source = new FileStream(
            path, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            source.SetLength(sourceSize);
            source.Seek(restartOffset, SeekOrigin.Begin);
            source.WriteByte((byte)'Z');
        }
        using TcpListener controlListener = StartListener();
        using TcpListener dataListener = StartListener();
        List<string> commands = new List<string>();
        List<string> uploads = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = controlListener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            Receive(stream, commands, "200 Type selected\r\n");
            Receive(stream, commands, "213 " + restartOffset.ToString() + "\r\n");
            uploads.Add(ReceiveResumedUpload(
                stream, dataListener, commands, restartOffset,
                "150 Opening resumed data\r\n", "226 Resume complete\r\n"));
        });
        FtpClient client = ConnectAuthenticated(controlListener);
        FtpTransferRequest request = new FtpTransferRequest();
        request.Direction = FtpTransferDirection.Upload;
        request.RemotePath = "/large.bin";
        request.LocalPath = path;
        request.Options.CollisionPolicy = FtpCollisionPolicy.Resume;
        request.Options.ResumePolicy = FtpResumePolicy.Required;
        request.Options.VerificationMode = FtpVerificationMode.None;
        request.HasRestartOffset = true;
        request.RestartOffset = restartOffset;
        request.HasExpectedSize = true;
        request.ExpectedSize = sourceSize;

        FtpTransferResult result = client.Transfer(request);
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands, Does.Contain("REST 3000000000"));
            Assert.That(uploads, Is.EqualTo(new[] { "Z" }));
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.RestartMarker, Is.EqualTo("3000000000"));
            Assert.That(result.ActualSize, Is.EqualTo(sourceSize));
            Assert.That(result.Progress.LogicalBytes, Is.EqualTo(sourceSize));
            Assert.That(result.Progress.WireBytes, Is.EqualTo(1));
        });

        client.AbortConnection();
        Directory.Delete(directory, true);
    }

    [Test]
    public void Upload_resume_uses_APPE_when_REST_STREAM_was_not_advertised()
    {
        using TcpListener controlListener = StartListener();
        using TcpListener dataListener = StartListener();
        List<string> commands = new List<string>();
        List<string> uploads = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = controlListener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            Receive(stream, commands, "200 Type selected\r\n");
            Receive(stream, commands, "213 6\r\n");
            uploads.Add(ReceiveUpload(
                stream, dataListener, commands, "150 Opening append\r\n",
                "226 Append complete\r\n"));
        });
        FtpClient client = ConnectAuthenticated(controlListener);
        client.State.Features.Negotiated = true;
        client.State.FeatureState.FileSize = true;
        client.State.FeatureState.RestStream = false;
        client.State.FeatureState.ExtendedPassive = true;
        using MemoryStream content = new MemoryStream(
            Encoding.ASCII.GetBytes("hello world"));
        FtpTransferRequest request = CreateStreamRequest(
            "/file.bin", content, FtpCollisionPolicy.Resume);
        request.Options.ResumePolicy = FtpResumePolicy.Required;

        FtpTransferResult result = client.Transfer(request);
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands, Is.EqualTo(new[]
            {
                "USER alice", "PASS secret", "TYPE I", "SIZE /file.bin",
                "EPSV", "APPE /file.bin",
            }));
            Assert.That(uploads, Is.EqualTo(new[] { "world" }));
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.RestartMarker, Is.Null);
            Assert.That(result.ActualSize, Is.EqualTo(11));
        });

        client.AbortConnection();
    }

    [Test]
    public void Required_upload_resume_refuses_an_unsafe_remote_length()
    {
        using TcpListener listener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = listener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            Receive(stream, commands, "200 Type selected\r\n");
            Receive(stream, commands, "213 20\r\n");
        });
        FtpClient client = ConnectAuthenticated(listener);
        using MemoryStream content = new MemoryStream(
            Encoding.ASCII.GetBytes("short source"));
        FtpTransferRequest request = CreateStreamRequest(
            "/file.bin", content, FtpCollisionPolicy.Resume);
        request.Options.ResumePolicy = FtpResumePolicy.Required;

        FtpTransferResult result = client.Transfer(request);
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.FailureMessage, Does.Contain("inconsistent"));
            Assert.That(commands, Is.EqualTo(new[]
            {
                "USER alice", "PASS secret", "TYPE I", "SIZE /file.bin",
            }));
            Assert.That(content.Position, Is.Zero);
        });

        client.AbortConnection();
    }

    [Test]
    public void IfSafe_upload_resume_falls_back_to_full_STOR_when_SIZE_fails()
    {
        using TcpListener controlListener = StartListener();
        using TcpListener dataListener = StartListener();
        List<string> commands = new List<string>();
        List<string> uploads = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = controlListener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            Receive(stream, commands, "200 Type selected\r\n");
            Receive(stream, commands, "550 SIZE unavailable\r\n");
            uploads.Add(ReceiveUpload(
                stream, dataListener, commands, "150 Opening data\r\n",
                "226 Transfer complete\r\n"));
        });
        FtpClient client = ConnectAuthenticated(controlListener);
        using MemoryStream content = new MemoryStream(
            Encoding.ASCII.GetBytes("complete"));
        content.Position = content.Length;
        FtpTransferRequest request = CreateStreamRequest(
            "/file.bin", content, FtpCollisionPolicy.Overwrite);
        request.Options.ResumePolicy = FtpResumePolicy.IfSafe;

        FtpTransferResult result = client.Transfer(request);
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands, Is.EqualTo(new[]
            {
                "USER alice", "PASS secret", "TYPE I", "SIZE /file.bin",
                "EPSV", "STOR /file.bin",
            }));
            Assert.That(uploads, Is.EqualTo(new[] { "complete" }));
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.ActualSize, Is.EqualTo(8));
        });

        client.AbortConnection();
    }

    [Test]
    public void Upload_resume_rejects_a_nonseekable_local_stream_before_network_use()
    {
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
        using NonSeekableReadStream content =
            new NonSeekableReadStream(Encoding.ASCII.GetBytes("content"));
        FtpTransferRequest request = CreateStreamRequest(
            "/file.bin", content, FtpCollisionPolicy.Resume);
        request.Options.ResumePolicy = FtpResumePolicy.Required;

        Assert.Multiple(() =>
        {
            Assert.That(
                () => client.Transfer(request),
                Throws.TypeOf<FtpConfigurationException>()
                    .With.Message.Contains("seekable"));
            Assert.That(commands, Is.EqualTo(new[] { "USER alice", "PASS secret" }));
        });

        client.AbortConnection();
    }

    private static FtpTransferRequest CreateStreamRequest(
        string remotePath,
        Stream content,
        FtpCollisionPolicy policy)
    {
        FtpTransferRequest request = new FtpTransferRequest();
        request.Direction = FtpTransferDirection.Upload;
        request.RemotePath = remotePath;
        request.Content = content;
        request.Options.CollisionPolicy = policy;
        request.Options.VerificationMode = FtpVerificationMode.None;
        return request;
    }

    private static string ReceiveUpload(
        NetworkStream controlStream,
        TcpListener dataListener,
        List<string> commands,
        string preliminaryReply,
        string finalReply)
    {
        commands.Add(ReadCommand(controlStream));
        WriteText(controlStream, "229 Entering Extended Passive Mode (|||" +
            GetPort(dataListener).ToString() + "|)\r\n");
        using TcpClient data = dataListener.AcceptTcpClient();
        commands.Add(ReadCommand(controlStream));
        WriteText(controlStream, preliminaryReply);
        string content;
        using (NetworkStream dataStream = data.GetStream())
        using (MemoryStream buffer = new MemoryStream())
        {
            dataStream.CopyTo(buffer);
            content = Encoding.ASCII.GetString(buffer.ToArray());
        }
        WriteText(controlStream, finalReply);
        return content;
    }

    private static string ReceiveResumedUpload(
        NetworkStream controlStream,
        TcpListener dataListener,
        List<string> commands,
        long restartOffset,
        string preliminaryReply,
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
        WriteText(controlStream, preliminaryReply);
        string content;
        using (NetworkStream dataStream = data.GetStream())
        using (MemoryStream buffer = new MemoryStream())
        {
            dataStream.CopyTo(buffer);
            content = Encoding.ASCII.GetString(buffer.ToArray());
        }
        WriteText(controlStream, finalReply);
        return content;
    }

    private static string CreateTemporaryDirectory()
    {
        string path = Path.Combine(
            Path.GetTempPath(), "ftp-upload-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
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

    private sealed class NonSeekableReadStream : Stream
    {
        private readonly MemoryStream inner;

        public NonSeekableReadStream(byte[] content)
        {
            inner = new MemoryStream(content);
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

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
            return inner.Read(buffer, offset, count);
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
            throw new NotSupportedException();
        }
    }
}
