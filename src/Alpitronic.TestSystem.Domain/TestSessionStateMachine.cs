
namespace Alpitronic.TestSystem.Domain;

/// <summary>
/// Stati del test
/// </summary>
public enum TestSessionState
{
    Created, LoadingSetup, Connecting, Starting, Monitoring,
    Evaluating, UploadingResult, Completed, Failed, Cancelled
}

/// <summary>
/// Impedisce transizioni incoerenti (ad esempio upload prima della valutazione).
/// Questa classe non esegue I/O: rappresenta esclusivamente l'invariante di dominio.
/// </summary>
public sealed class TestSessionStateMachine
{
    public TestSessionState State { get; private set; } = TestSessionState.Created;

    public void MoveTo(TestSessionState next)
    {
        if (!IsAllowed(State, next))
            throw new InvalidOperationException($"Invalid transition {State} -> {next}.");
        State = next;
    }

   /// <summary>
   /// Validazione della transizione
   /// </summary>
   /// <param name="current">Stato di partenza</param>
   /// <param name="next">Stato di destinazione</param>
   /// <returns></returns>
    private static bool IsAllowed(TestSessionState current, TestSessionState next) => (current, next) switch
    {
        (TestSessionState.Created, TestSessionState.LoadingSetup) => true,
        (TestSessionState.LoadingSetup, TestSessionState.Connecting) => true,
        (TestSessionState.Connecting, TestSessionState.Starting) => true,
        (TestSessionState.Starting, TestSessionState.Monitoring) => true,
        (TestSessionState.Monitoring, TestSessionState.Evaluating) => true,
        (TestSessionState.Evaluating, TestSessionState.UploadingResult) => true,
        (TestSessionState.UploadingResult, TestSessionState.Completed) => true,
        _ => false
    };

    //Stati terminali
    public void Fail() => State = TestSessionState.Failed;
    public void Cancel() => State = TestSessionState.Cancelled;

}
