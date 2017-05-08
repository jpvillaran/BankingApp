# BankingApp

## Overview
- This application requires DotNet Core 1.1.1.
- This application made use of the Microsoft Identity framework for the User logins.  A separate DataContext (making use of the same database) was used for the authentication mechanism.

## Required Nuget installations to run the application
- Microsoft.AspNetCore (1.1.1)
- Microsoft.AspNetCore.Authentication.Cookies (1.1.1)
- Microsoft.AspNetCore.Identity.EntityFrameworkCore (1.1.1)
- Microsoft.AspNetCore.Mvc (1.1.2)
- Microsoft.AspNetCore.Razor.Tools
- Microsoft.AspNetCore.StaticFiles (1.1.1)
- Microsoft.EntityFrameworkCore (1.1.1)
- Microsoft.EntityFrameworkCore.SqlServer (1.1.1)
- Microsoft.Extensions.Logging.Debug (1.1.1)
- Microsoft.NETCore.App (1.1.1)

## Required Nuget installations for unit testing
### Note: Other libraries were taken from an alternate Nuget source.  Please add https://www.myget.org/F/aspnet-contrib/api/v3/index.json to the package sources.

- Microsoft.EntityFrameworkCore (1.1.1)
- Moq (4.7.9) <-- This came from the alternate package source site
- Microsoft.EntityFrameworkCore.InMemory (1.1.1) - Not used
- System.Diagnostics.TraceSource (4.0.0)
- Microsoft.NET.Test.Sdk (15.0.0)
- Microsoft.NETCore.App (1.1.1)
- xunit (2.2.0)
- xunit.runner.visualstudio (2.2.0)

## Database

This application used the built-in SQL Server local database that came with the Visual Studio 2017 Installation.
Since there are two data contexts used for the application, you will have to do the following to install the database:

Using a command prompt, run the following on the BankingApp project directory (not the solution director):

```command prompt
dotnet restore

dotnet ef database update --context BankingContext

dotnet ef database update --context BankingIdentityContext
```


