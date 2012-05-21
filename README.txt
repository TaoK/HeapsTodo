         * HeapsTodo format rules
         * 
         * HeapsTodo tasks are closely based on todo.txt, but with one major difference (and a few smaller ones):
         * In HeapsTodo, a task is NOT necessarily limited to a single line. This one detail breaks the cardinal
         * rule of the todo.txt format, and means that HeapsTodo needs its own rules.
         * 
         * The todo.txt format, which HeapsTodo is loosely based on, is specified here:
         * https://github.com/ginatrapani/todo.txt-cli/wiki/The-Todo.txt-Format
         * 
         * Here are the four major DIFFERING rules of HeapsTodo tasks:
         * 
         * 1) A task is USUALLY limited to a single line, but it can have a "Notes" section which can span multiple lines.
         *  The "Notes" section of a task should usually be at the end of the task (if it is not, automated tools will always 
         *  put it there when rewriting / updating the task), and is bounded by SOME DELIMITER WHICH IS STILL TO BE DEFINED, 
         *  PROBABLY THREE BACKTICKS.
         * 
         * 2) Tasks are hierarchical - one task can have multiple sub-tasks, and those can have further sub-tasks, etc. 
         *  The relationship between the tasks is indicated by indentation: A task is a sub-task of the previous task if is 
         *  further indented. Tabs and spaces are both acceptable for indentation, and if a mixture is used then tabs 
         *  will be considered equivalent to 4 spaces. Programs are expected to handle these indentations consistently 
         *  RULES HERE. The default should be 2 spaces.
         *  
         * 3) The todo file may contain comments - comments are lines that start with a hash/number sign ("#"), outside of 
         *  the scope of a task "Notes" section. Comments are to be ignored when displaying/rendering a todo file as a set of 
         *  tasks or synchronizing a file with some other system, but should not be deleted from the file by any automated 
         *  processing. Commands MAY be moved, however, so they should never assume a position with respect to any given 
         *  task; they are really only useful at the top of a todo file.
         * 
         * 4) Like completed tasks are specified with a leading "x", incomplete tasks can OPTIONALLY have a leading hyphen
         *  "-". This can help with legibility when editing large complex todo lists with word wrap enabled, and/or files
         *  that contain many "Notes". The user should decide whether the leading hyphen should be automatically added or 
         *  not. (either way, the file will always work with any HeapsTodo-format-compliant program).
         * 
         * Beyond these specifications, the HeapsTodo format is effectively the same as todo.txt:
         *  
         * - A task may optionally start with a priority, a single uppercase letter in parens. (so you cannot have 
         *  more than 26 priority levels!)
         * - After the optional priority, a task may have an optional creation date.
         * - Dates are always specified in ISO-standard format, YYYY-MM-DD.
         * - A task is "Complete"/done if the first non-whitespace character is a lowercase "x".
         * - A task's completion date is a date specified immediately after the initial "x". This means that if you're 
         *  keeping track of task creation dates, you MUST also specify completion dates, because your creation dates 
         *  will otherwise be misinterpreted as completion dates (unless you also used priorities :))
         * - Contexts are specified with an initial "@" sign (and include everything to the next whitespace or end-of-
         *  line), and can feature anywhere in the task body.
         * - Projects are specified with an initial "+" sign (and include everything to the next whitespace or end-of-
         *  line), and can feature anywhere in the task body.
         *  
         * These simple rules mean that any line of text can be a todo item (unless it is in a Notes section), and any 
         *  violations of the format rules (for example, having an uppercase X at the start or a creation date in another 
         *  date format) simply mean that text will be taken to be part of the task name/body. Is is not possible to 
         *  cause a "Parsing Failure" in todo.txt or HeapsTodo files, any text file is a legal (if likely 
         *  garbled/uninteligible) todo file.
         *  
         * Also, any sensible todo.txt also works with HeapsTodo, there is no "import" process necessary because 
         *  the HeapsTodo format is a superset/extension of todo.txt.
         *  
         * This "library" is still missing a lot of stuff:
         *  - Documentation
         *  - Thread-safety
         *  - File-Handling
         *  - Change-detection (and handling approach)
         *  - Preference-handling, esp. "todo.txt-compatible mode" disabling extensions
