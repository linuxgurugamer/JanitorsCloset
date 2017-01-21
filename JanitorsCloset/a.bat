c:\local\jq-win64  ".VERSION.MAJOR" JanitorsCloset.version >tmpfile
set /P major=<tmpfile

c:\local\jq-win64  ".VERSION.MINOR" JanitorsCloset.version >tmpfile
set /P minor=<tmpfile

c:\local\jq-win64  ".VERSION.PATCH" JanitorsCloset.version >tmpfile
set /P patch=<tmpfile

c:\local\jq-win64  ".VERSION.BUILD" JanitorsCloset.version >tmpfile
set /P build=<tmpfile
del tmpfile