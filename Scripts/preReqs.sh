#!/bin/bash


if [$(which jq zip curl | wc -l) = 0 ]; then
    sudo apt install jq zip curl
     echo "installing jq zip curl using apt"
 else
    echo " jq zip curl already installed"
fi

if  [ $( which az   | wc -l) = 0  ]; then
    curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash
else
    echo "az already installed"
fi

if [ $( dotnet --list-sdks | grep -G ^7.*$sdk | wc -l ) = 0 ]; then
    curl -sL  https://dotnet.microsoft.com/download/dotnet/scripts/v1/dotnet-install.sh > dotnet-install.sh
    chmod +x ./dotnet-install.sh
    echo "Installing net7"
    ./dotnet-install.sh --channel 7.0
else
    echo "dotnet already installed. SDKs:"
    dotnet --list-sdks
fi


if  [ $( which dotnet   | wc -l) = 0  ]; then
    if  [ $(  grep "#DOTNET path" ~/.profile   | wc -l) = 0  ]; then
        echo "adding dotnet path to .profile"
        touch ~/.profile
        echo "#DOTNET path" >> ~/.profile
        echo "PATH=\$PATH:\$HOME/.dotnet:\$HOME/.dotnet/tools" >> ~/.profile
        source ~/.profile 
        exit 1
    else
        echo "Already in .profile"
    fi    
else
    echo "dotnet already in path"
fi





