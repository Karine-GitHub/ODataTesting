# ODataTesting
Create a sample project to consume a OData service that requires authentication by certificate

The WebAPI uses a trusted self-signed certificate.
You have to replace "df74ee9108fd3a258511ed2d287e195ce6a50b0d" with the thumbprint of your certificate.

If you don't have one, you can generate it by following these instructions:
https://docs.microsoft.com/en-us/dotnet/core/additional-tools/self-signed-certificates-guide#with-dotnet-dev-certs
