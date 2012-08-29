
## HeapsTodo

This is a project based on / derived from todo.txt:

* It comprises a library and set of utilities / programs for managing todo files
* The todo files are entirely text-based
* Most of the utilities / functionality will work both with classic "todo.txt" format, and the extended HeapsTodo format
* The classic todo.txt format is documented at http://todotxt.com/
* The extended HeapsTodo format is documented below

This project is AGPL-licensed, see the details in the section below and accompanying LICENSE.txt file.

### Goals 

This project aims to both provide additional tools for the todo.txt community (eg sync to google tasks),
and start/support a new community of HeapsTodo users. 

The tools that will be developed include:

* A .Net library for easily interacting with todo.txt and HeapsTodo files
* A command-line sync tool for automatic / transparent synching (and merging changes) to and from Google Tasks
* A UI program based on Ben Hughes' todo.txt.net for interacting with todo.txt and HeapsTodo files cross-platform (Windows, OSX, linux)
* (probably) An Android touch client based on todo.txt-touch, for handling HeapsTodo files on Android and supporting proper reliable sync.

### HeapsTodo File Format

HeapsTodo tasks are closely based on todo.txt, but with one major difference (and a few smaller ones):
In HeapsTodo, a task is NOT necessarily limited to a single line. This one detail breaks the cardinal
rule of the todo.txt format, and means that HeapsTodo needs its own rules.

The todo.txt format, which HeapsTodo is loosely based on, is specified here:
https://github.com/ginatrapani/todo.txt-cli/wiki/The-Todo.txt-Format

Here are the four major DIFFERING rules of HeapsTodo tasks:

1.  A task is USUALLY limited to a single line, but it can have a "Notes" section which can span multiple lines.
    The "Notes" section of a task should usually be at the end of the task (if it is not, automated tools will always 
    put it there when rewriting / updating the task), and is bounded by SOME DELIMITER WHICH IS STILL TO BE DEFINED/FINALIZED,
    PROBABLY THREE BACKTICKS.
2.  Tasks are hierarchical - one task can have multiple sub-tasks, and those can have further sub-tasks, etc.
    The relationship between the tasks is indicated by indentation: A task is a sub-task of the previous task if is
    further indented. Tabs and spaces are both acceptable for indentation, and if a mixture is used then tabs
    will be considered equivalent to 4 spaces. Programs are expected to handle these indentations consistently,
    RULES TO BE CLEARLY EXPLAINED HERE. The default should be 2 spaces.
3.  The todo file may contain comments - comments are lines that start with a hash/number sign ("#"), outside of
    the scope of a task "Notes" section. Comments are to be ignored when displaying/rendering a todo file as a set of
    tasks or synchronizing a file with some other system, but should not be deleted from the file by any automated
    processing. Commands MAY be moved, however, so they should never assume a position with respect to any given
    task; they are really only useful at the top of a todo file.
4.  Like completed tasks are specified with a leading "x", incomplete tasks can OPTIONALLY have a leading hyphen
    "-". This can help with legibility when editing large complex todo lists with word wrap enabled, and/or files
    that contain many "Notes". The user should decide whether the leading hyphen should be automatically added or
    not. (either way, the file will always work with any HeapsTodo-format-compliant program).

Beyond these specifications, the HeapsTodo format is effectively the same as todo.txt:

* A task may optionally start with a priority, a single uppercase letter in parens. (so you cannot have more than 26 priority levels!)
* After the optional priority, a task may have an optional creation date.
* Dates are always specified in ISO-standard format, YYYY-MM-DD.
* A task is "Complete"/done if the first non-whitespace character is a lowercase "x".
* A task's completion date is a date specified immediately after the initial "x". This means that if you're keeping track of task creation dates, you MUST also specify completion dates, because your creation dates will otherwise be misinterpreted as completion dates (unless you also used priorities :))
* Contexts are specified with an initial "@" sign (and include everything to the next whitespace or end-of-line), and can feature anywhere in the task body.
* Projects are specified with an initial "+" sign (and include everything to the next whitespace or end-of-line), and can feature anywhere in the task body.

These simple rules mean that any line of text can be a todo item (unless it is in a Notes section), and any
violations of the format rules (for example, having an uppercase X at the start or a creation date in another
date format) simply mean that text will be taken to be part of the task name/body. Is is not possible to
cause a "Parsing Failure" in todo.txt or HeapsTodo files, any text file is a legal (if likely garbled/uninteligible) todo file.

Also, any sensible todo.txt also works with HeapsTodo, there is no "import" process necessary because the HeapsTodo format is a superset/extension of todo.txt.
To have your todo.txt file recognized as a HeapsTodo file, and unlock the HeapsTodo features on your file, just add an extra comment line
at the start of the file with the text "HeapsTodo Task List":

    #HeapsTodo Task List


### Progress / Status

The Library is still missing a lot of stuff:

* Documentation
* Thread-safety
* File-Handling
* Change-detection (and handling approach)
* Ambiguity-handling / source-preservation

The Sync Tool is coming along, and is almost ready for "production" use for both HeapsTodo and todo.txt files.
Things still missing include:

* Ambiguity handling (see library)
* Code organization - much code that should be shared is now in the commandline tool source
* Comprehensive logging - by default the tool should keep a comprehensive log of all changes made, for debugging.
* Creation Date handling - for some reason task creation date never made it...
* Fixing submodule references to use a public/unathenticated URL
* Adding handling for cert mismatches, for new mono installs
* Determining how to use DPAPI in mono/linux, 
** looks like "mozroots" creates folders with incorrect security
** we don't yet know whether using DPAPI first will correct this
** even if it does work, they might have incorrect setup from previous mozroots setup, so need messaging
* Determining how to ILMerge DotNetOpenAuth in a linux mono/MonoDevelop build environment - DotNetOpenAuthMini maybe?
* adding safe / appropriate handling for SSL
** Probably complex to handle
*** move up DPAPI to BEFORE SSL calls
*** add messaging for DPAPI issues
*** properly try/catch SSL block
*** offer mozroots solution
*** also offer to just ignore, configurably
* adding friendly handling for URL-spawn failure (xdg-utils on linux, otherwise???)
* generalizing linux instructions beyond ubuntu/debian/mint

The UI program / client is not yet started - it will be the next phase of the project once the Sync Tool
is published and more-or-less complete.

### License

This library and set of applications is released under the GNU Affero GPL v3: 
http://www.gnu.org/licenses/agpl.txt

The homepage for this project is currently: 
https://github.com/TaoK/HeapsTodo

This project uses several external libraries:

* NDesk.Options, for command-line parsing: The NDesk.Options library is licensed under 
    the MIT/X11 license, and its homepage is here: http://www.ndesk.org/Options
* ILRepack, for assembly merging: ILRepack is licensed under the Apache License 2.0, 
    and its homepage is here: https://github.com/gluck/il-repack
* Synchrotron's Diff implementation (MIT License), Copyright Tony Garnock-Jones and
    LShift Ltd., ported to C# by Tao Klerks.
* Google's API Client Library for .Net, licensed under the Apache License 2.0, 
    homepage at http://code.google.com/p/google-api-dotnet-client/
* Log4Net (via Google API Client Library), Apache License 2.0
* JSON.Net (via Google API Client Library), MIT License
* DotNetOpenAuth (via Google API Client Library), MS-PL
* NUnit, for automated testing. NUnit is licensed under a custom open-source license
    based on the zlib/libpng license, and its homepage is: http://www.nunit.org/

I am open to relicensing the project under a more liberal license (eg Apache 2.0), but for the moment
have no reason to do so - if you think I should, please tell me!

### Feedback

As always, feedback is very welcome, plase contact me at tao@klerks.biz.

Tao Klerks
