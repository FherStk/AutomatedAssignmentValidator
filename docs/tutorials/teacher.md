# Teacher's guide
## How to create a new script
A script is a set of calls to AutoCheck's connectors in order to perform CRUD operations and validate its results, so all the scripts are defined as YAML files and should be as simplier as possible in order to improve readability. Notice that LocalShell will be the implicit connector so each command used over an undefined connector will be directly passed through the local shell and executed into the local computer enviroment.

A set of commands can be executed within a question and, if all the expected values matches with the current ones, the question will score; otherwise, this question wont score and the execution continues in order to execute the rest of the defined script.

Scoring values and also the captions and messages displayed within the terminal output are self-managed by AutoCheck, however some of this data can be manually overriden or modified when needed.

## Examples
### Runing a local terminal command
```
name: "Local command example (with no vars)"

single:
    local:
        folder: "\\home\\usuario\\folder\\"

body:        
  - question: 
      description: "Checking for file generation"
      content:                                  

        - run:
            caption:  "Looking for the file... "
            command:  "test -e \\home\\usuario\\folder\\myfile.txt && echo 1 || echo 0"
            expected: "=1"
                  
        - run:
            caption:  "Looking for file content..."
            command:  "echo {$CURRENT_FOLDER_PATH}\\myfile"                  
            expected: "=%contains this sentence%"

```

### Runing a local terminal command using vars
```
name: "Local command example (with vars)"

single:
    local:
        folder: "\\home\\usuario\\folder\\"

vars:
    my_file: "myfile.txt"

body:        
  - question: 
      description: "Checking for file generation"
      content:                                  

        - run:
            caption:  "Looking for the file... "
            command:  "test -e {$CURRENT_FOLDER_PATH}\\{$MY_FILE} && echo 1 || echo 0"
            expected: "=1"
                  
        - run:
            caption:  "Looking for file content..."
            command:  "echo {$CURRENT_FOLDER_PATH}\\{$MY_FILE}"                  
            expected: "=%contains this sentence%"

```

### Running an AutoCheck connector's command:
```
name: "AutoCheck's HTML connector example"
body:
  - connector:            
      type: "Html"        
      arguments: "--folder {$CURRENT_FOLDER_PATH} --file index.html"       
        
  - question: 
      description: "Checking index.html"                                
      content:                            

        - run:
            caption:  "Validating document against the W3C validation service... "
            connector: "Html"
            command:  "ValidateHtml5AgainstW3C"            
            onexception: "SKIP"

        - question:                     
            description: "Validating headers"
            content:                            
              - run:
                  caption:  "Checking amount of level-1 headers... "
                  connector: "Html"
                  command:  "CountNodes"
                  arguments: "--xpath //h1"                  
                  expected: ">=1"

              - run:
                  caption:  "Checking amount of level-2 headers... "
                  connector: "Html"
                  command:  "CountNodes"
                  arguments: "--xpath //h2"                  
                  expected: ">=1"
```

### Running a remote command:

```
name: "Remote command example (with no vars)"
body:   
- connector:            
      type: "Shell"    
      name: "RemoteShell"    
      arguments: "--remoteOS GNU --host 192.168.1.196 --username user --password pwd"    

  - question: 
      description: "Checking for file generation"
      content:                                  

        - run:
            caption:  "Looking for the file... "
            connector: "RemoteShell"
            command:  "test -e \\home\\usuario\\folder\\myfile.txt && echo 1 || echo 0"
            expected: "=1"
                  
        - run:
            caption:  "Looking for file content..."
            connector: "RemoteShell"
            command:  "echo {$CURRENT_FOLDER_PATH}\\myfile"                  
            expected: "=%contains this sentence%"

```

## Script nodes and hierarchy
There are different nodes types than can be used within an AutoCheck's YAML script file:

### Scalar
A scalar node means a primitive value, so no children allowed.

In the following example, `type` and `arguments` are scalar nodes:
```
type: "Html"
arguments: "--folder {$CURRENT_FOLDER_PATH} --file index.html"
```

### Collection
A collection node means a collection of other nodes, so children are allowed but those cannot be repeated.

In the following example, `vars` is a collection:
```
vars:     
  student_name: "Fer"
  student_var: "{$STUDENT_NAME}"
  student_replace: "This is a test with value: {$STUDENT_NAME}_{$STUDENT_VAR}!" 
```

### Sequence
A sequence node means a collection of other nodes, so children are allowed and also those can be repeated.

In the following example, `body` is a sequence because its children uses `-` before the node name, but each `run` node is a collection:
``` 
body:     
  - run:
      command: "echo {$STUDENT_NAME}"
      expected: "demo"
  
  - run:
      command: "echo {$STUDENT_REPLACE}"
      expected: "This is a test with value: Demo!"

  - run:
      command: "echo {$TEST_FOLDER}"
      expected: "TEST_FOLDER"

  - run:
      command: "echo {$FOLDER_REGEX}"
      expected: "FOLDER"
```

## Script nodes definition
The following guide describes the allowed YAML tags within each level:

#### root
Root-level tags has no parent.

Name | Type | Mandatory | Description | Default
------------ | -------------
version | text | no | The script version. | `1.0.0.0`
name | text | no | The script name will be displayed at the output. | `Current file's name`
caption | text | no | Message to display at script startup. | `Running script {$SCRIPT_NAME} (v{$SCRIPT_VERSION}):`
max-score | decimal | no | Maximum script score (overall score). | `10`
[output](#output) | collection | no | Setups the output behaviour. | 
[vars](#vars) | collection | no | Custom global vars can be defined here and refered later as `$VARNAME`, allowing regex and string formatters. | 
[pre](#pre) | sequence | no | Defined blocks will be executed (in order) before the body. |
[post](#post) | sequence | no | Defined blocks will be executed (in order) before the body. |
[body](#body) | sequence | no | Script body. |

### <a name="output"></a> output
Setups the output behaviour.

Name | Type | Mandatory | Description | Default
------------ | -------------
terminal | boolean | no | When enabled, all log mesages will be directed to the standard output (terminal). | `True`
pause | boolean | no | When enabled, a terminal pause will be produced between each batch execution (batch-mode only). | `True`
[log](#log) | collection | no | Allows storing log data into external files. | 

#### <a name="log"></a> log
Setups the output file-mode behaviour.

Name | Type | Mandatory | Description | Default
------------ | -------------
enabled | boolean | no | When enabled, all log mesages will be stored into external files: a single one for single-executed scripts; individual files for batch-executed scripts. | `False`
folder | text | no | Path to the folder which will contain the log data. | `{$APP_FOLDER_PATH}\\logs\\`
name | text | no | The name that will be used to store each file, so vars should be used in order to create single files per batch execution (only on batch mode). | `{$SCRIPT_NAME}_{$NOW}`

### <a name="vars"></a> vars
Custom global vars can be defined wihtin `vars` node and refered later as `$VARNAME`, allowing regex and string formatters.

#### Predefined vars:
##### Application data:
Name | Type | Description
------------ | -------------| -------------
APP_FOLDER_NAME | text | The root app execution folder (just the folder name). 
APP_FOLDER_PATH | text | The root app execution folder (the entire path). 

##### Script data:
Name | Type | Description
------------ | -------------| -------------
SCRIPT_NAME | text | The current script name. 
SCRIPT_FOLDER_NAME | text | The current script's containing folder (just the folder name).
SCRIPT_FOLDER_PATH | text | The current script's folder (the entire path). 
SCRIPT_FILE_NAME | text | The current script file name.
SCRIPT_FILE_PATH | text | The current script file path.
SCRIPT_CAPTION | text | The main script caption.
SINGLE_CAPTION | text | The single-mode script caption.
BATCH_CAPTION | text | The batch-mode script caption.
NOW | datetime | The current datetime in UTC format including the offset like '"2021-03-09T03:30:46.7775027+00:00"'

##### Log data:
Name | Type | Description
------------ | -------------| -------------
LOG_FOLDER_NAME | text | The current log folder (just the folder name).
LOG_FOLDER_PATH | text | The current log folder (the entire path).
LOG_FILE_NAME | text | The current log file (the file name).
LOG_FILE_PATH | text | The current log file (the entire path).

##### Execution data:
Name | Type | Description
------------ | -------------| -------------
EXECUTION_FOLDER_NAME | text | The current script's execution folder (just the folder name). 
EXECUTION_FOLDER_PATH | text | The current script's execution folder (the entire path).
RESULT | text | The result of the last `command` execution.

##### Question and score data:
Name | Type | Description
------------ | -------------| -------------
CURRENT_QUESTION | decimal | The current question (and subquestion) number (1, 2, 2.1, etc.)
CURRENT_SCORE | decimal | The current question (and subquestion) score.
TOTAL_SCORE | decimal | The computed total score, it will be updated for each question close.
MAX_SCORE | decimal | The maximum score available.

##### Batch mode data (local and remote):
Name | Type | Description
------------ | -------------| -------------
CURRENT_TARGET | text | Returns the kind of the current batch execution: `none`, `local` or `remote`.
CURRENT_FOLDER_NAME | text | The folder name where the script is targeting right now (local or remote); can change during the execution for batch-typed.
CURRENT_FOLDER_PATH | text | The folder path where the script is targeting right now (local or remote); can change during the execution for batch-typed.
CURRENT_FILE_NAME | text | The folder name where the script is targeting right now (local or remote); can change during the execution for batch-typed.
CURRENT_FILE_PATH | text | The folder path where the script is targeting right now (local or remote); can change during the execution for batch-typed.
REMOTE_OS | [GNU; WIN; MAC] | Only for remote batch mode: the remote OS family for the current remote batch execution.
REMOTE_HOST | text | Only for remote batch mode: the host name or IP address for the current remote batch execution.
REMOTE_USER | text | Only for remote batch mode: the username for the current remote batch execution.
REMOTE_PORT | number | Only for remote batch mode: the ssh port for the current remote batch execution.
REMOTE_PASSWORD | text | Only for remote batch mode: the password for the current remote batch execution.
REMOTE_FOLDER_NAME | text | An alias for CURRENT_FOLDER_NAME
REMOTE_FOLDER_PATH | text | An alias for CURRENT_FOLDER_PATH
REMOTE_FILE_NAME | text | An alias for CURRENT_FILE_NAME
REMOTE_FILE_PATH | text | An alias for CURRENT_FILE_PATH

#### Custom example vars:
Vars can be combined within each other by injection, just call the var with `$` and surround it between brackets `{}` when definint a text var like in the following examples:
```
vars:
    var1: "VALUE1"
    var2: "This is a sample text including {$VAR1} and also {$SCRIPT_NAME}"    
    var3: !!bool False
    var4: !!int 1986    
```

#### Regular expressions over vars example:
Regular expressions (regex) can be used to extract text from an original var, just add the regex expression before calling the var surrounding it between a hashtag `#` and a dollar `$` like in the following examples:
```
vars:    
    var1: "VALUE1"
    var2: "PRE_POST"
    var3: "This is the result of applying a regular expression to var1: {#regex$VAR1}"
    var4: "This will display the last word after an underscore: {#(?<=_)(.*)$VAR2}"
    var5: "This will display the last folder for a given path: {$CURRENT_FOLDER_NAME}"        
```

#### Scopes:
Because `vars` can be used at different script levels, those vars will be created within its own scope being accessible from lower scopes but being destroyed once the scope is left:
```
vars:
    var1:  "Value1.1" #Declares a var1 within the current scope or updates the current value.
    $var1: "Value1.2" #Does not declares but updates var1 value on the first predecesor scope where the var has been found.
```

When requesting a var value (for example: {$var1}) the scope-nearest one will be retreived, so shadowed vars will be not accessible; it has been designed as is to reduce the scope logic complexity, simple workarounds can be used as store the value locally before shadowing or just use another var name to avoid shadowing.

### <a name="pre"></a> pre
Defined blocks will be executed (in order) before the script's body does.

#### <a name="extract"></a> extract
Extract a compressed file from the current execution folder (only zip is supported).

Name | Type | Mandatory | Description | Default
------------ | -------------
file | text | no | Search patthern used to find files for extraction, OS file naming convetions allowed; regex can be used also. | `"*.zip"`
remove | boolean | no | Remove the original file when extracted. | `False`
recursive | boolean | no | Repeat through folders. | `False`

#### <a name="restore_db"></a> restore_db
Restores a database using an sql dump file from the current execution folder (only PostgreSQL is supported).

Name | Type | Mandatory | Description | Default
------------ | -------------
file | text | no | Search patthern used to find files to restore, OS file naming convetions allowed; regex can be used also. | `"*.sql"`
remove | boolean | no | Remove the original file when restored. | `False`
recursive | boolean | no | Repeat through folders. | `False`
db_host | text | no | Host name or IP address. | `"localhost"`
db_user | text | no | Credential's username. | `"postgres"`
db_pass | text | no | Credential's password. | `"postgres"`
db_name | text | no | Database name. | `The current script name ("$SCRIPT_NAME")`
override | boolean | no | Overrides the DB if exists. | `False`

#### <a name="upload_gdrive"></a> upload_gdrive
Uploads a file from the current execution folder to Google Drive.

Name | Type | Mandatory | Description | Default
------------ | -------------
source | text | no | Search patthern used to find files or folders to upload, OS file naming convetions allowed; regex can be used also. | `"*.sql"`
remove | boolean | no | Remove the original file when uploaded. | `False`
recursive | boolean | no | Repeat through folders. | `False`
link | boolean | no | A link to the source file will be extracted from text file's content. | `False`
copy | boolean | no | The source file will be copied directly to gdrive (when possible) instead of downloaded and re-uploaded. | `True`
account | text | no | Path to a file containing the username used to login into the own's Google Drive account. | `"config\\gdrive_account.txt"`
secret | text | no | Path to the `client_secret.json` file that will be used to login into the own's Google Drive account (it can be generated through the Google API Console services). | `"config\\gdrive_secret.json"`
remote_path | text | no | Where to upload the files; the remote folders will be created if needed and files, if no filename has been specified, will be auto-named using the original names when possible. | `"\\AutoCheck\\scripts\\{$SCRIPT_NAME}\\"`
remote_file | text | no | The remote file will be created using this value as a template, but original extension will be preserved. | 

### <a name="post"></a> post
Defined blocks will be executed (in order) after the script's body does; same nodes as `pre` are allowed.

### <a name="body"></a> body
Script's body where the main action is defined.

Name | Type | Mandatory | Description 
------------ | -------------
[vars](#vars) | collection | no | Defines vars in the same way and with the same rules as the ones defined within root level, but as local-scope vars; useful to store command results or other stuff.
[connector](#connector) | collection | no | Defines a connector to use, it can be defined wherever inside the body (usually inside a question's content). 
[run](#run) | collection | no | Runs a command, it can be used wherever inside the body (usually inside a question's content).
[question](#question) | collection | no | Defines a question to test and score; can be repeated.
echo | text | no | Displays a message.

#### <a name="vars"></a> vars
Defines vars in the same way and with the same rules as the ones defined within root level, but as local-scope vars; useful to store command results or other stuff.

#### <a name="connector"></a> connector
Defines a connector to use, it can be defined wherever inside the body (usually inside a question's content).

Name | Type | Mandatory | Description | Default
------------ | -------------
caption | text | no | Message to display before running, when running with no caption means working in silent mode so, if a connector is created within a question, it only computes for a question score when a caption has been displayed because computing hidden results would be confusing when reading a report. | 
type | text | no | Which connector will be used (see avaliable connectors through API documentation). | `"LOCAL_SHELL"`
name | text | no | Name that will be used by a `run` node to execute a connector's command. | `The same as "type" node value.`
[arguments](#arguments) | text | no | As terminal app will do (--arg1 val1 --arg2 val2 ... --argN valN). | 
success | text | no | If a caption has been defined, this message witll be shown if the executed command result matches with the expected one. | `"OK"`
error | text | no | If a caption has been defined, this message witll be shown if the executed command result mismatches with the expected one; it will be followed by a list of errors found. | `"ERROR"`
onexception | text | no | Determines the behaviour on exception when creating a connector; allowed values are: <ul><li>*SUCCESS*: continues as no error.</li><li>*ERROR*: continues as an error.</li><li>*ABORT*: stops the entire script execution.</li><li>*SKIP*: stops the current question execution but continues with the next one.</li></ul>Only works when a caption has been defined, because working on silent mode never computes for a question score nor produces any output. | `"ERROR"`

#### <a name="run"></a> run
Runs a command, it can be used wherever inside the body (usually inside a question's content).

Name | Type | Mandatory | Description | Default
------------ | -------------
caption | text | no | Message to display before running, when running with no caption means working in silent mode so, if run is performed within a question, it only computes for a question score when a caption has been displayed because computing hidden results would be confusing when reading a report. | 
connector | text | no | Previously defined connector name, which will be used to run the command. If no connector has been defined within this `run`, the nearest within the scope will be looked for (and envelopping scopes recursively) and, if no connector is found, a `LOCALSHELL` one will be used. | `"LOCALSHELL"`
command | text | yes | The command to run, the result will be stored as `$RESULT`; can be a shell command if no connector has been specified. | 
[arguments](#arguments) | text | no | Same as `connector` ones (also typed arguments list are allowed). | 
expected | text | no | Expected value from the run command, which can be: <ul><li>Variables</li><li>Typed data: <ul><li>`True`</li><li>`75.7`</li><li>`"Example"`</li></ul></li><li>Regular expression: <ul><li>`{#regex$VARNAME}`</li></ul><li>SQL-like opperators: <ul><li>`>=75.1`</li><li>`%substring%`</li><li>`%endwith`</li><li>`LIKE %{#regex$CURRENT_FOLDER_NAME}%`</li></ul></li><li>Arrays opperators: <ul><li>`LENGTH =x`</li><li>`CONTAINS >=x`</li><li>`UNORDEREDEQUALS [x,y,z]`</li><li>`ORDEREDEQUALS [x,y,z]`</li></ul></li></ul> When not defined, all results will compute as a success; when working on silent mode (with no caption), an exception will be thrown on mismatch (onexception wont applicate). **Warning: no AND/OR combinations still supported.** | 
success | text | no | If a caption has been defined, this message witll be shown if the executed command result matches with the expected one. | `"OK"`
error | text | no | If a caption has been defined, this message witll be shown if the executed command result mismatches with the expected one; it will be followed by a list of errors found. | `"ERROR"`
onexception | text | no | Determines the behaviour when a command execution ends with an exception; allowed values are: <ul><li>*SUCCESS*: continues as no error.</li><li>*ERROR*: continues as an error.</li><li>*ABORT*: stops the entire script execution.</li><li>*SKIP*: stops the current question execution but continues with the next one.</li></ul>Only works when a caption has been defined, because working on silent mode never computes for a question score nor produces any output. | `"ERROR"`
onerror | text | no |  Determines the behaviour when a command execution ends with an error (including onexception=ERROR), which means that the output missmatched with the expected value; allowed values are: <ul><li>*CONTINUE*: continues with the script regular execution.</li><li>*ABORT*: stops the entire script execution.</li><li>*SKIP*: stops the current question execution but continues with the next one.</li></ul>Only works when a caption has been defined, because working on silent mode never computes for a question score nor produces any output. | `"CONTINUE"`
store | text | no | The value of `$RESULT` will be stored into a new var within the current scope or an existing one if has already defined (for example: `MY_VAR`), storing into upper scopes is also allowed by adding the dollar `$` symbol before the varname (for example: `$MY_VAR`). | 

#### <a name="question"></a> question
Defines a question to test and score; can be repeated.

Name | Type | Mandatory | Description | Default
------------ | -------------
score | decimal | no | Question's socre, ignored if subquestions are used (question within question). | `!!float 1`
caption | text | no | Question's caption that will be displayed at the output. | `"Question {$CURRENT_QUESTION} [{$CURRENT_SCORE}]:"`
description | text | no | Question description, it will be displayed after the question caption. | 
[content](#content) | sequence | yes | What to test within the question, all must be ok to compute the score; cannot be mandatory due subquestion behaviour; can be repeated.

##### <a name="content"></a> content
What to test within the question, all must be ok to compute the score; cannot be mandatory due subquestion behaviour; can be repeated.

Name | Type | Mandatory | Description 
------------ | -------------
[vars](#vars) | collection | no | Same as `vars` defined within `body`, but as local-scope vars; useful to store command results or other stuff.
[connector](#connector) | collection | no | A connector can be also defined here; see `connector` definition within `body`.
[question](#question) | collection | no | A sub-question can also be defined here (parent score will be updated as the summary of its children); see `question` definition within `body`.
[run](#run) | collection | yes | Same as `run` defined within `body`, but all the executed commands within a question's content must succeed (no execution errors and result matching the expected value) in order to compute the entire score.  
echo | text | no | Displays a message.

### <a name="arguments"></a> arguments
Arguments can be defined in different ways and can be requested within the script as `$CONNECTOR_NAME.ARGUMENT_NAME`:

#### inline arguments:
As terminal app will do (--arg1 val1 --arg2 val2 ... --argN valN):
```
- connector:            
      type: "Html"        
      arguments: "--folder {$CURRENT_FOLDER_PATH} --file index.html"    
```

#### typed arguments:
Also, arguments can be defined using data types:
```
- connector:            
      type: "Html"        
      arguments: 
        folder: "{$CURRENT_FOLDER_PATH}"
        file: "index.html"    
```

Notice than typed arrays are also supported, but all the items must be of the same type:
```
- connector:            
      type: "Demo"        
      arguments: 
        arg1:
          - item: !!int 1
          - item: !!int 3
          - item: !!int 0
```

And also typed collections are supported, but all the keys must be of the same type (not the values):
```
- connector:            
      type: "Demo"        
      arguments: 
        arg1: 
          - item:
              key:   "key1"
              value: "value1"
          - item:
              key:   "key2"
              value: "value2"
          - item:
              key:   "key3"
              value: "value3"
```

## Single mode execution
The main idea of a single-typed script is to use a generic template and set a single destination to run over it, it can be accomplished using inheritance and defining the target data at root level.

Name | Type | Mandatory | Description | Default
------------ | -------------
inherits | text | no | Relative path to a YAML script file; any script can inherit from any other and overwrite whatever it needs. | `"NONE"`
[single](#single) | collection | no | Single mode definition. | 

So running this script will load the template data (the inherited one) and will add (and override if needed) the single script data.

## Batch mode execution
The main idea of a batch-typed script is to iterate through a set of destinations and run a single-typed script over it, so inherits behaviour has been designed in order to allow the same code execution over a single target or a batch one just by adding this values at root level.

Name | Type | Mandatory | Description | Default
------------ | -------------
inherits | text | no | Relative path to a YAML script file; any script can inherit from any other and overwrite whatever it needs. | `"NONE"`
[batch](#batch) | sequence | no | Batch mode definition. | 

Script definition can be defined here exactly as a single-typed script, however, inheriting from a single-typed script is strongly recommended.

Recommended structure: 
<ul>
    <li><i>main_script.yaml</i>: as neutral so no folder or batch definition
        <ul>
            <li><i>single_script.yaml</i>: inherits and defines 'single' property</li>
            <li><i>batch_script.yaml</i>: inherits and defines 'batch' property</li>
        </ul>
    </li>
</ul>

### <a name="single"></a> single
Batch mode definition.

Name | Type | Mandatory | Description | Default
------------ | -------------
caption | text | no | Message to display before the single execution. | `Running on single mode:`
[local](#local) | collection | yes (if no `remote` has been defined) | Local single target, so the script body will be executed over the local target. | 
[remote](#remote) | collection | yes (if no `local` has been defined) | Remote single target, so the script body will be executed over the remote target. | 

### <a name="batch"></a> batch
Batch mode definition.

Name | Type | Mandatory | Description | Default
------------ | -------------
caption | text | no | Message to display before every batch execution. | `"Running on batch mode:"`
[copy_detector](#copy_detector) | collection | no | Enables the copy detection logic, not supported for `host` targets. | 
[local](#local) | sequence | yes (if no `remote` has been defined) | Local batch target, so each script body will be executed once per local target. | 
[remote](#remote) | sequence | yes (if no `local` has been defined) | Remote batch target, so each script body will be executed once per remote target. | 

#### <a name="copy_detector"></a> copy_detector
Enables the copy detection logic, not supported for `host` targets (see avaliable copy detectors through API documentation). Just a single file per folder can be loaded into the copy detector engine, but this will be upgraded in a near future in order to allow multi-file support. 

Name | Type | Mandatory | Description | Default
------------ | -------------
type | text | yes |The type of copy detector to use (see avaliable copy detectors through API documentation). | 
file | text | no | Search patthern used to find files for extraction, OS file naming convetions allowed; regex can be used also. The first file found using the search pattern will be loaded into the copy detector engine.| `"*"`
caption | text | no | Message displayed at output before every check. | `"Looking for potential copies within {$CURRENT_FOLDER_NAME}..."`
threshold | decimal | no | The copy threshold to use, so results exceeding this value will be considered as a pontential copy. | `!!float 1 `
[pre](#pre) | sequence | no | Defined blocks will be executed (in order, once per target) before the copy detector execution. |
[post](#post) | sequence | no | Defined blocks will be executed(in order, once per target) after the copy detector execution. |

#### <a name="local"></a>local
Local batch target, so each script body will be executed once per local target.

Name | Type | Mandatory | Description | Default
------------ | -------------
path | text | no (if no `folder` has been defined) | The script will be executed once for each local folder contained within the defined path; the current folder can be requested through the script with `$CURRENT_FOLDER_PATH` | 
folder | text | yes (if no `path` has been defined) | The script will be executed once for each local folder defined; the current folder can be requested through the script with `$CURRENT_FOLDER_PATH` |
[vars](#vars) | collection | no | Custom global vars can be defined here and refered later as `$VARNAME`, allowing regex and string formatters.| 

#### <a name="remote"></a>remote
Remote batch target, so each script body will be executed once per remote target.

Name | Type | Mandatory | Description | Default
------------ | -------------
os   | [GNU; WIN; MAC] | no | The remote OS family | `GNU`
host | text | yes | The script will be executed once for each defined host address or name, be aware that **defining a range of hosts is still not supported**, but the `remote` block can be repeated if needed. | 
user | text | yes | The username used to connect with the remote host. | 
password | text | no | The password used to connect with the remote host. | (Blank password)
port   | number | no | The remote SSH port used to connect with. | 22
path | text | no | The script will be executed once for each folder contained within the defined remote path; the current folder can be requested through the script with `$CURRENT_FOLDER_PATH` | 
folder | text | no | The script will be executed once for each remote folder defined; the current folder can be requested through the script with `$CURRENT_FOLDER_PATH` |
[vars](#vars) | collection | no | Custom global vars can be defined here and refered later as `$VARNAME`, allowing regex and string formatters. | 
