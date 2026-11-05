Mapping table documenting supported FTP commands and the corresponding API..

*Connection commands*

| Command  	    		| API						| Description                  	|
|---------------		|-----------				|---------------------------	|
| **HOST**  			| SendHost and<br>SendHostDomain	| Identify your domain on shared hosting|
| **USER, PASS**  		| Credentials				| Login with username & password|
| **QUIT**  			| Disconnect()				| Disconnect	|
| **PASV, EPSV, EPRT**  | DataConnectionType		| Passive & Active FTP modes	|
| **FEAT**  			| HasFeature()				| Get the features supported by server |
| **SYST**  			| GetSystem()				| Get the server system type 	|
| **OPTS UTF8 ON**  	| Encoding 					| Enables UTF-8 filenames	|
| **OPTS UTF8 OFF**  	| Encoding, DisableUTF8() 	| Disables UTF-8 filenames	|
| **AUTH TLS**  		| EncryptionMode			| Switch to TLS/FTPS 	|
| **PBSZ, PROT**  		| EncryptionMode and<br>DataConnectionEncryption | Configure TLS/FTPS connection 	|
| **CCC**				| PlainTextEncryption		| Switch to plaintext FTP |
| **PRET**      		| *Automatic* 				| Pre-transfer file information |
| **TYPE A**  			| UploadDataType and<br>DownloadDataType	| Transfer data in ASCII	|
| **TYPE I**  			| UploadDataType and<br>DownloadDataType 	| Transfer data in Binary	|

*File Management commands*

| Command      			| API					| Description                  	|
|---------------		|-----------			|---------------------------	|
| **MLSD**  			| GetListing()			| Get directory machine list 	|
| **LIST**  			| GetListing() with FtpListOption.ForceList		| Get directory file list 	|
| **NLST**  			| GetNameListing()<br>GetListing() with FtpListOption.ForceNameList	| Get directory name list 	|
| **LS**  				| GetListing() with FtpListOption.UseLS		| Get directory file list 	|
| **STAT**  			| GetListing() with FtpListOption.UseStat		| Get directory file list 	|
| **MLST**				| GetObjectInfo()		| Get file information			|
| **DELE**      		| DeleteFile()			| Delete a file |
| **MKD**      			| CreateDirectory() 	| Create a directory |
| **SITE MKDIR**      	| CreateDirectory() 	| Create a directory with server-side recursion (ProFTPD) |
| **RMD**      			| DeleteDirectory() 	| Delete a directory |
| **SITE RMDIR**      	| DeleteDirectory() 	| Delete a directory with server-side recursion (ProFTPD) |
| **RMDA**      		| DeleteDirectory() 	| Delete a directory with server-side recursion (Serv-U) |
| **CWD**      			| SetWorkingDirectory() | Change the working directory |
| **PWD**      			| GetWorkingDirectory() | Get the working directory |
| **SIZE**      		| GetFileSize() 		| Get the filesize in bytes |
| **MDTM**   			| GetModifiedTime()<br>GetListing() with FtpListOption.Modify<br>GetObjectInfo() with dateModified | Get the file modified date  |
| **MFMT**   			| SetModifiedTime()		 | Modify file modified date  |
| **SITE CHMOD**      	| Chmod() or SetFilePermissions() | Modify file permissions |
| **RNFR, RNTO**   		| Rename()<br>MoveFile()	| Rename a file or directory<br>Move a file or directory  |
| **NOOP**   			| Upload()<br>UploadFile()<br>Download()<br>DownloadFile()	| Keep-alive used during file transfer  |

*File Hashing commands*

| Command      			| API							| Description                  	|
|---------------		|-----------					|---------------------------	|
| **HASH**  			| GetChecksum() 					| Gets the hash of a file using various algorithms	|
| **MD5**  				| GetChecksum()		| Gets the MD5 hash of a file	|
| **XMD5**  			| GetChecksum()	| Gets the MD5 hash of a file	|
| **XSHA1**  			| GetChecksum()| Gets the SHA-1 hash of a file	|
| **XSHA256**  			| GetChecksum()| Gets the SHA-256 hash of a file	|
| **XSHA512**  			| GetChecksum()	| Gets the SHA-512 hash of a file	|
