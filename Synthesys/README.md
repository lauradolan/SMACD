# Synthesys - Service Map Vulnerability Scanner

## Building
To provide the best possible cross-platform experience, Synthesys runs on NET Core 3.0.

### Prerequisites
* NET Core SDK 3.0.100
* Visual Studio 2019 **Preview**

Synthesys should build as committed in most cases through Visual Studio, unless otherwise noted in the commit message. One debug feature that is used in Synthesys allows a developer to manually specify "command line arguments" during debugging. This prompt does not normally appear otherwise, unless using `SMACD_DEBUG=1` in the command line.

## Usage
Before you can use Synthesys, you need to [create a Service Map](GetStarted.md). Once you've done that, you can run Synthesys:

```shellscript
$ ./Synthesys scan -s servicemap.yaml -o session
```

The output of this scan will be serialized to an archive called `session`.

## Logging
By default, Synthesys runs at a log level of "Information". You can increase or decrease the log level by applying `-l <level>` or `--loglevel <level>` to the command above.

* 0: Verbose
* 1: Debug
* 2: Information
* 3: Warning
* 4: Error
* 5: Fatal

If you want to remove branding and program messages, you can specify `-z` or `--silent` *as well*. That means if you want complete silence except for errors, you can run with `-l 5 -z`

## Thresholds
Extensions in Synthesys generate scores based on the errors discovered. Since this number is based on the way the application is created, it's wise to test the application several times as a baseline to ascertain an appropriate theshold.

Once you understand the maximum threshold for issues, you can specify it on the command line with `-t` or `--threshold`. If the threshold is exceeded, the application will exit with a `-1` code; this is to help with automation (such as a CI/CD pipeline).

## Feature/Use Case Constraints
The scanner can constrain its tests to a specific Feature or Use Case by specifying its name with `--feature` or `--usecase`. When specifying `--usecase`, you must give both the Feature name and Use Case name: `--usecase myFeature//myUseCase`. This limits the scanning to only those items in the Service Map.

## Limit to Known Items Constraint
If the developer does not want the scanner to go exploring other parts of the application based on information it sees, the scanner can be run with `-k` or `--limitknown`. This will only maintain nodes in the AppTree based on what nodes are created during the import of the Service Map.

## Service Map Generator
Synthesys can generate a sample Service Map with data from [Bogus](https://github.com/bchavez/Bogus), if needed:

```shellscript
$ ./Synthesys generate -s newservicemap.yaml -m 3
```

This command will output a new Service Map to `newservicemap.yaml` with a maximum of `3` elements to create in each generation of the example map. All of the generated Abuse Cases will point to the `dummy` Extension.

The generated Service Map can be fed *directly* into the Scan function to generate a sample Session file based on the dummy module.

## Snoop Mode
Synthesys allows users to investigate the state of the Extensions registered in the scanner. For example:

```shellscript
$ ./Synthesys snoop

 ┏━┓┏┳┓┏━┓┏━╸╺┳┓ ╭┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉╌╌┄┄┈┈
 ┃  ┃┃┃┃ ┃┃   ┃┃ ┇   Synthesys - Service Map Scanning Tool
 ┗━┓┃┃┃┣━┫┃   ┃┃ ┇   Version 1.0.0 build xyzzy
   ┃┃┃┃┃ ┃┃   ┃┃ ┇   github.com/anthturner/SMACD
 ┗━┛╹ ╹╹ ╹┗━╸╺┻┛ ╰┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉┉╌╌┄┄┈┈
 
HOST ENVIRONMENT:
  Synthesys 1.0.0.0 running on COMPUTER running Unix 4.19.57.0
 
LOADED LIBRARIES:
· /SMACD/Synthesys/Plugins/Synthesys.Plugins.Dummy.dll      Synthesys.Plugins.Dummy, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null      
  ACTIONS:
  └─ dummy runs Synthesys.Plugins.Dummy.DummyAction
  REACTIONS:
  └─ Synthesys.Plugins.Dummy.DummyReaction via Extension Trigger (dummy Succeeds)
```