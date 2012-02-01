
namespace PSFilterLoad.PSApi
{ 
    /// <summary>
    /// The delegate the TestAbortProc can call for PDN to tell it to abort
    /// </summary>
    /// <returns>The value of IsCancelRequested as a byte.</returns>
    internal delegate byte AbortFunc();
}