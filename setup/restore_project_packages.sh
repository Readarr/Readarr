#!/bin/bash

# See https://github.com/dotnet/sdk/issues/3803 #

directory="../src/"

for i in $(find $directory -type f -name '*.csproj'); do
    nuget restore $i
done