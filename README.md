# BlackHole-Core-Cli

A dotnet tool that is the Command Line Interface for [BlackHole-Core-ORM](https://github.com/Mikarsoft/BlackHole-Core-ORM)

You can install this as a dotnet global tool and then run it from the command line of your solution folder, or
from the package manager console in visual studio. You need to have the BlackHole Core ORM included in your
project in order for this cli to work.

To run this Cli you need to type 'bhl' and then the command that you wish.

It supports 3 basic commands:

First Command:
  - update  => Example: 'bhl update'
      Update command reads the 'BlackHoleEntities' in your project and the connection string and it generates or updates the database
      based on these.
    
      Important: Create the Entities and configure BlackHole before running this command.
    
      If you accidently delete a property from your 'BlackHoleEntities' the cli will not delete the column in the database. It will set
      it to Nullable.
      If you wish to delete unused columns from the database, then delete the corresponding properties from the BlackHoleEntities and then
      run 'bhl update -f' or 'bhl update --force' to force a strict update on the database. 
      If BlackHole is in developer mode then the --force argument is used by default from the Cli.

      Also in case you need to keep a history of the database updates, BlackHole Cli can store the changes in Sql files to the
      selected default Datapath, by using the argument '-s' or '--save' after the command. Example : 'bhl update -s'

      You can use both '--force' and '--save' arguments in the same command. Example 'bhl update -f -s'

Second Command:
  - drop  => Example: 'bhl drop'

BlackHole Example:

 -Find an Example Project here => [BlackHole Example](https://github.com/Mikarsoft/BlackHole-Example-Project)
 
BlackHole Documentation:

 -Find Online Documentation here => [BlackHole Documentation](https://mikarsoft.com/BHDocumentation/index.html)
