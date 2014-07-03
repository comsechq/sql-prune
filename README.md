## SQL Pruning Utility

A simple utility written in .NET to to prune MS-SQL backup files from a given folder (or an Amazon S3 bucket).

The program lists all Microsoft SQL Server `.bak` files in a given folder and determines which one to keep and which ones to prune.

#### Disclaimer

**WARNING**: This program is designed to delete files from your computer. The authors of this program do not accept liability for any errors or data-loss which could arise as a result of it.

#### Pruning Rules

1. Keep one week of daily backups
2. One Sunday of backups for a four weeks
3. One backup per month for a year
4. One backup per year after that
6. When more than one backup per day, keep the most recent.
5. Prune anything else

**Notes:** The rules apply **per database** and only support the Julian calendar.

Example of expected file names (as automatically generated by backup plans created in MS SQL management studio):

    dbname1_backup_2014_06_20_010002_0897411.bak
    dbname2_backup_2014_06_20_010002_0957417.bak
    ...

The utility relies on the date *in the file name*, **not** the file system's creation date.

#### Usage:

    sqlprune.exe [pathToFolder] [-verbose] [-delete] [-no-confirm]

 * __pathToFolder__ The path to your local folder containting .bak files (e.g. "c:\sql-backups")
 * __-verbose__ Writes more information to the standard output
 * __-delete__ Unless this flag is present files will not be deleted
 * __-no-confim__ You will have to confirm before any file is deleted unless this flag is present

#### Todo:

* Add S3 support: handle local paths or Amazon S3 bucket URLs.

#### Download & Install:

1. Find the [latest release](https://github.com/comsechq/sql-prune/releases).
2. Extract the zip in a folder.
3. Run the command from the command line prompt.

#### Unit Testing

Veryfying which date should be kept or pruned is a tedious task. 

We modified an [SVG visualiser](http://bl.ocks.org/mbostock/4063318) to ouput renders the output of the unit tests into familiar calendar view.

After you run the unit tests in `PruneServiceTest`, open `Calendar.html` in a modern web browser.

_If your web browser doesn't have access to you local file system (e.g. Chrome) it will refuse to load the .json file._

Example:

![alt tag](https://raw.githubusercontent.com/comsechq/sql-prune/master/unit-test-output-example.png)