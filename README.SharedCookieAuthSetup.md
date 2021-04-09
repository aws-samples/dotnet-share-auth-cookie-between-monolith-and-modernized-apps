# Summary

This document walks through the steps to setup a shared cookie-based authentication that works between the ASP.NET and the ASP.NET Core.

## Step 1: Add Nuget packages
- ### ASP.NET 4.6.1
    - Add the following Nuget package
        ```csharp
        Microsoft.Owin.Security.Interop``` // Provides the compatibility layer for sharing authentication tickets between Microsoft.Owin
        ```    
    - Ensure the following Nugest packages are referenced:
        ```csharp
        Microsoft.Owin.Host.SystemWeb // Enables the OWIN middleware to hook into the IIS request pipeline.
        Microsoft.Owin.Security.Cookies // Enables cookie based authentication.
        ```

- ### ASP.NET Core
    - Add the following Nuget package 
        ```csharp 
        Microsoft.Owin.Security.Interop
        ```
## Step 2: Add IXmlRepository custom implemtnation

In a distributed system, we need a central repository to store the Data Protection key that is used to encrypt/decrypt the cookie. For our use case, we will use the AWS Parameter Store as the central repository to store the Data Protection key.  Also, out of the box, the <em>Data Protection</em> functionality does not provide an implementation that works with the AWS parameter store.  Thus, we need to implement the <em>IXmlRepository</em> interface for Data Protection to work with the AWS parameter store. 

- ### ASP.NET 4.6.1
    Reference the <em>\Modernization.Series\Legacy.Monolith\Services\CustomPersistKeysToAWSParameterStore.cs</em> for implementation details.
    
- ### ASP.NET Core
    Reference the <em>\Modernization.Series\Modernized.Backend.ServiceA\Services\CustomPersistKeysToAWSParameterStore.cs</em> for implementation details.

## Step 3: Add & configure authentication middlware

- ### ASP.NET 4.6.1

    Add the following to the <em>'Startup.Auth.cs'</em> file:

    <details>
        <summary> Sample code </summary>

    ```csharp
    #region [Custom]: add the auth middleware & configure it for shared cookie.

    ... From within the <em>'Startup.Auth.cs'</em>, locate this region and copy its content...

    #endregion
    ```
    </details>
    <br/>

- Ensure the same scheme name that you choose (e.g. Identity.Application) is used for generating the user's identity as well. 

    <details>
        <summary> Example below </summary>

    ```csharp
    var identity = new ClaimsIdentity(
        new[] {
            new Claim(ClaimTypes.Name, "Admin"),
            new Claim(ClaimTypes.Email, "admin@admin.com")
        },
        "Identity.Application");
    ```
    </details>
    <br/>

- ### ASP.NET Core (w/out the ASP.NET Identity)

    Add the following to the <em>Startup.cs</em> file:

    <details>
        <summary> Sample code </summary>

    ```csharp
    #region [Custom]: Add the auth cookie support & configuring for Shared cookie.
    ... From within the <em>'Startup.cs'</em>,  locate this region and copy its content...
    #endregion
    ```
    </details>
