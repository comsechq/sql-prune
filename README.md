## SQL Pruning Utility

A simple utility written in .NET to to prune MS-SQL backup files from a given folder (or an Amazon S3 bucket).

The program lists all Microsoft SQL Server `.bak` files in a given folder or Amazon S3 bucket and determines which one to keep or prune.

#### Disclaimer

**WARNING**: This program is designed to delete files. The authors of this program do not accept liability for any errors or data-loss which could arise as a result of it.

#### Pruning Rules

1. Keep daily backups for two weeks
2. Keep One Sunday per week for eight weeks
3. Keep 1st and 3rd Sunday of each month for 52 weeks
4. Keep 1st Sunday of each year after that
6. When more than one backup per day, keep the most recent.
5. Prune anything else

**Notes:** 

The following pruning rules apply:

- Per **database**
- Only support the _Julian_ calendar 
- Apply from the _date of the most recent backup_ for a given database

Example of expected file names (as automatically generated by backup plans created in Microsoft SQL Management Studio):

    dbname1_backup_2014_06_20_010002_0897411.bak
    dbname2_backup_2014_06_20_010002_0957417.bak
    ...

The utility relies on the date *in the file name*, **not** the file system's creation date.

#### Usage:

##### Pruning Mode:

    sqlprune.exe [path] -prune [-delete] [-no-confirm] [-aws-profile]

 * __path__ is the path to a local folder or an S3 bucket containting .bak files (e.g. `c:\sql-backups` or `s3://bucket-name/backups`)
 * __-prune__: The flag to activate the 'prune' mode
 * __-delete__ is a flag you must add otherwise files will not be deleted
 * __-file-extensions__ is an optional parameter can use to restrict to different file extensions (see [File Extensions](#file-extensions))
 * __-no-confim__ is flag you can use if you don't want to confirm before any file is deleted 
 * __-AWSProfileName__ is optional and can be used to override the value of the `AWSProfileName` app setting (see S3 Credentials)
 * __-AWSProfilesLocation__ is optional and can be used to override the value of the `AWSProfilesLocation` app setting (see S3 Credentials)

Examples:

Simply list which `.bak` files would be pruned in a folder without deleting anthing (dry run):

    sqlprune E:\Backups

Confirm before deleting prunable backups in `E:\Backups`, including sub directories:

    sqlprune E:\Backups -delete

Confirm before deleting prunable backups for database names starting with `test` in `s3://bucket-name`:

    sqlprune s3://bucket-name/test -delete

##### Recovery Mode:

    sqlprune.exe [path] -recover -db-name -dest [-date] [-no-confirm]

 * path: The path to a local folder or an S3 bucket containting .bak files (e.g. \"c:\\sql-backups\" or \"s3://bucket-name/backups\")");
 * __-recover__: The flag to activate the 'recovery' mode
 * __-db-name__: The exact name of the database to recover (case sensitive)
 * __-dest__: The path to a local folder where to copy the file to
 * __-date-time__: OptionallySpecifies which date and time to retrieve
 * __-date__: OptionallySpecifies which date to retrieve
 * __-file-extensions__ is an optional parameter can use to restrict to different file extensions (see [File Extensions](#file-extensions))
 * __-no-confim__ is flag you can use if you don't want to confirm before any file is recovered
 * __-aws-profile__ is optional and defaults to the value of the `AWSProfileName` app setting (see S3 Credentials)

When multiple .bak files are found the most recent is used.

Examples:

Copy the most recent `.bak` backup available for the database `helloworld` from an S3 bucket:

    sqlprune S3://bucket-name/test -recover -db-name helloworld -dest E:\Backups

Copy `helloWorld_backup_2014_06_20_010002_0957417.bak` from `E:\Backups` to the `C:\destination`:

    sqlprune E:\Backups -recover -db-name helloWorld -date 2014-06-20T01:00:02 -dest C:\destination

#### Download & Install:

1. Find the [latest release](https://github.com/comsechq/sql-prune/releases).
2. Extract the zip in a folder.
3. Run the command from the command line prompt.

#### File Extensions

By default the `prune` and `recover` commands restrict to files ending with `.bak` extension. 

You can override this search pattern with the `-matchExpression` parameter. Comma separated values can be used.

Example:

- Use a different file extension for your backup files: `-file-extensions .backup`
- Match on multiple file extensions for your backup files: `-file-extensions .bak,.bak.7z,.bak.rar`

#### S3 Credentials

You can ignore this completely if you just want to prune files from a local folder.

sqlprune.exe loads the amazon credentials it needs to connect to S3 a
[configuration file](http://docs.aws.amazon.com/cli/latest/userguide/cli-chap-getting-started.html) located in `~/.aws/config`, using the `default` profile.

Example:

    [default]
    aws_access_key_id = XXXXXXXXXXXXXXXXXXXX
    aws_secret_access_key = YYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYY

You can have more than one profile in your configuration file.

To override the default profile name, either modify the `AWSProfileName` setting  in `sqlprune.exe.config` or alternatively add an `-AWSProfileName` parameter when you execute `sqlprune.exe` from the command line.

You can also modify the `AWSProfilesLocation` setting in `sqlprune.exe.config` to load a different file (e.g. "c:\users\you\.aws\credentials").

When not set, AWSProfileName defaults to `default` and AWSProfilesLocation defaults to `c:\users\[current_user]\.aws\config`.

#### TODO:

- Cutomisable pruning rules: 
    - Handle recovering to an S3 bucket as well as a local path
    - Load the rules from an XML configuration file
    - Generate an XSD that describes the XML syntax for pruning rules 
    - The pruning rules in the configuration file are applied one after the other

#### Unit Testing

Veryfying which date should be kept or pruned is a tedious task.

To make it easier, we have modified an [SVG visualiser](http://bl.ocks.org/mbostock/4063318) 
to render the output of the unit tests into a familiar calendar view.

After you run the unit tests in `PruneServiceTest`, open `Calendar.html` in a modern web browser.

_If your web browser doesn't have access to you local file system (e.g. Chrome) it will refuse to load the .json file._

Example:

![alt tag](https://raw.githubusercontent.com/comsechq/sql-prune/master/unit-test-output-example.png)

####License

This project is licensed under the terms of the [MIT license](https://github.com/comsechq/sql-prune/blob/master/LICENSE.txt). 

By submitting a pull request for this project, you agree to license your contribution under the MIT license to this project.
