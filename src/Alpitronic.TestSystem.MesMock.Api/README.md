# MES Mock API

Questo progetto è una **Minimal API ASP.NET Core locale**: non implementa il MES reale, ma espone il contratto REST consumato da `HttpMesGateway`.

```powershell
dotnet run --project .\src\Alpitronic.TestSystem.MesMock.Api\Alpitronic.TestSystem.MesMock.Api.csproj --urls http://localhost:5088
```

La descrizione completa di endpoint, status code, payload, idempotenza e collegamento WinUI è nella [documentazione REST MES](../../docs/mes-rest-api.md).

> Mantenere questo host separato dalla WinUI evita che una dimostrazione del protocollo diventi una dipendenza necessaria per eseguire la demo offline.
