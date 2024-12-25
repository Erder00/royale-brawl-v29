## Android
1: get it in [Github Actions](https://github.com/Erder00/royale-brawl-v29/actions)

2: install [Termux](https://github.com/termux/termux-app) and grant storage permission
```
termux-setup-storage
```

3: upgrade Packages
```
pkg update && pkg upgrade
```
4: install glibc and mariadb
```
apt install glibc-repo wget mariadb
apt install glibc-runner libicu-glibc
```
5: start mariadb
```
mariadbd-safe --datadir='/data/data/com.termux/files/usr/var/lib/mysql'
```
6: create new terminal sessions and run [mariadb-secure-installation](https://mariadb.com/kb/en/mariadb-secure-installation/)
```
mariadb-secure-installation
NOTE: RUNNING ALL PARTS OF THIS SCRIPT IS RECOMMENDED FOR ALL MariaDB
      SERVERS IN PRODUCTION USE!  PLEASE READ EACH STEP CAREFULLY!

In order to log into MariaDB to secure it, we'll need the current
password for the root user. If you've just installed MariaDB, and
haven't set the root password yet, you should just press enter here.

Enter current password for root (enter for none):
OK, successfully used password, moving on...

Setting the root password or using the unix_socket ensures that nobody
can log into the MariaDB root user without the proper authorisation.

Enable unix_socket authentication? [Y/n] n
 ... skipping.

Set root password? [Y/n] y
New password:
Re-enter new password:
Password updated successfully!
Reloading privilege tables..
 ... Success!


By default, a MariaDB installation has an anonymous user, allowing anyone
to log into MariaDB without having to have a user account created for
them.  This is intended only for testing, and to make the installation
go a bit smoother.  You should remove them before moving into a
production environment.

Remove anonymous users? [Y/n] y
 ... Success!

Normally, root should only be allowed to connect from 'localhost'.  This
ensures that someone cannot guess at the root password from the network.

Disallow root login remotely? [Y/n] y
 ... Success!

By default, MariaDB comes with a database named 'test' that anyone can
access.  This is also intended only for testing, and should be removed
before moving into a production environment.

Remove test database and access to it? [Y/n] y
 - Dropping test database...
 ... Success!
 - Removing privileges on test database...
 ... Success!

Reloading the privilege tables will ensure that all changes made so far
will take effect immediately.

Reload privilege tables now? [Y/n] y
 ... Success!

Cleaning up...

All done!  If you've completed all of the above steps, your MariaDB
installation should now be secure.

Thanks for using MariaDB!
```
7: start glibc runner
```
grun -s
```
8: Download dotnet and install 
```
wget https://download.visualstudio.microsoft.com/download/pr/501c5677-1a80-4232-9223-2c1ad336a304/867b5afc628837835a409cf4f465211d/dotnet-runtime-8.0.11-linux-arm64.tar.gz
mkdir .dotnet
tar xvf dotnet-runtime-8.0.11-linux-arm64.tar.gz -C .dotnet
grun -f .dotnet/dotnet
grun -c .dotnet/dotnet
```
9: Unzip Server
```
mkdir Server
cd Server
unzip "zip directory"
unzip Supercell.Laser.Server.1.0.0.zip
```
10: mariadb shell
```
mariadb -u root -p
Enter password: root password
CREATE DATABASE databasename;
exit;
```
11: import [database.sql](../database.sql)
```
wget https://github.com/Erder00/royale-brawl-v29/raw/refs/heads/main/database.sql
```
```
mariadb -u root -p"root password" databasename < database.sql
```
or
```
mariadb -u root -p databasename
Enter password: root password
source database.sql
exit;
```
12: change config.json
```
nano config.json
```
change the mysql_password to the password you set, and the mysql_database to the database you created. 
Control + x and y and Enter x2
### Run 
terminal 1
```
mariadbd-safe --datadir='/data/data/com.termux/files/usr/var/lib/mysql'
```
terminal 2 
```
grun -s
cd Server
~/.dotnet/dotnet Supercell.Laser.Server.dll
```
