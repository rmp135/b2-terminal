## B2Terminal

Work in progress utility to browse Backblaze B2 buckets in a similar way to an FTP client.

Long term supported operations include navigating the bucket structure, downloading files, and uploading files.

**Current Status**

- LS
  - List the currently available buckets, or files within that bucket, or directory.
- LLS
  - List the currently available local files in the local working directory.
- CD
  - Navigate into a bucket or directory.
  - Currently only supports a single level of navigation up or down. 
- PWD
  - Print the current remote working directory.
- LPWD
  - Print the local working directory.
- GET
  - Download a file in the current remote directory to the local working directory.
- PUT
  - Upload a file in the current local directory to the remote working directory.

**Usage**

Run the program with the `--accountID` and `--applicationKey` as arguments. 