# Game Server Node

Game Server Node is a .NET Core 3.1 Console Application acting as a server host, handling multiple gameservers via requests pulled from the centralised API.

## Windows Requirements

TODO

## Ubuntu 16.04 Requirements
(rewrite required for all common ubuntu versions, debian & centos... .NET is complete fuckery to linux)

### .NET Core 3.1 Runtime

#### Add Microsoft repository key and feed
Before installing .NET, you'll need to:

* Add the Microsoft package signing key to the list of trusted keys.
* Add the repository to the package manager.
* Install required dependencies.

Open a terminal and run the following commands.

```
wget https://packages.microsoft.com/config/ubuntu/16.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
```

#### Install the .NET Core runtime
Update the products available for installation, then install the .NET Core runtime. In your terminal, run the following commands.

```
sudo apt-get update
sudo apt-get install apt-transport-https
sudo apt-get update
sudo apt-get install dotnet-runtime-3.1
```

### SteamCMD

#### Prerequisites (64bit)

If you're using a 64 bit machine, you need to add Multiverse and i386 Architecture. In your terminal, run the following commands.

```
sudo add-apt-repository multiverse
sudo dpkg --add-architecture i386
sudo apt update
sudo apt install lib32gcc1
```

#### Install SteamCMD 

Install SteamCMD. In your terminal, run the following commands.

```
sudo apt install steamcmd
```

#### Installation

Create Directory, and set File Permissions

```
sudo mkdir /home/adam/servernode
sudo chown -R youruser:youruser /home/adam/servernode
sudo chmod 777 /home/adam/servernode
```
