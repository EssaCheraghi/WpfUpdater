this is Windopws Applications Updater Library that can use for any application that you work with it there is no need to implement updater Server And Client Sides,Just Follow Instruction To Setup This library To your project

1-Deploy UpdaterService Project To Your Host

2-Run UpdateServiceInitializer App in your local machine (use step 1 endpoint for it) and at Updates folder in server

            a-Create Folder With AppName And Prefix Id Number like (1-MyApplication)
            inside app folder

            b-create txt file named New_Features and write some thing there
            
            c-Inside Folder Create Folder (files) and copy new files for update there
            
            d-create empty txt file named init
            
            f-ready to call by clients

3-Reference updater.dll to your target application and use it like sample app

How It Works :

as you initialize an application in server by UpdateServiceInitializer app , it will create json file to manage information about files and then it will create parts for each files
then when clients wants to update thier apps they also download json and retrieve parts and then concat them , in this flow this library keeps modular updating and many benefits like resume update and 
live updating (any changes in files by owner in the server will detect after run initializer runs) 






