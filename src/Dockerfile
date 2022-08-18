#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["NET6_Template_Consumer_Console/NET6_Template_Consumer_Console.csproj", "NET6_Template_Consumer_Console/"]
COPY ["NET6_Template/NET6_Template.csproj", "NET6_Template/"]
RUN dotnet restore "NET6_Template_Consumer_Console/NET6_Template_Consumer_Console.csproj"
COPY . .
WORKDIR "/src/NET6_Template_Consumer_Console"
RUN dotnet build "NET6_Template_Consumer_Console.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "NET6_Template_Consumer_Console.csproj" -c Release -o /app/publish

FROM base AS final
STOPSIGNAL SIGINT
WORKDIR /app
COPY --from=publish /app/publish .
RUN mkdir log
ENV TZ=$(TIMEZONE)
RUN ln -snf /usr/share/zoneinfo/$TZ /etc/localtime && echo $TZ > /etc/timezone
EXPOSE 5000
ENTRYPOINT ["dotnet", "NET6_Template_Consumer_Console.dll"]