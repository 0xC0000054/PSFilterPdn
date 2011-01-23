
namespace PSFilterLoad.PSApi
{ 
    /// <summary>
    /// The delegate the TestAbortProc can call for PDN to tell it to abort
    /// </summary>
    /// <returns>The value of IsCancelRequested</returns>
    internal delegate bool abort();
}