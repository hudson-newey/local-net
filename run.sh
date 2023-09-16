#!/bin/bash

if [ ! -d ./local-net/bin/Release/ ]; then
    dotnet build -c Release ./local-net/local-net.csproj;
fi

./local-net/bin/Release/net7.0/local-net;
