
namespace PSFilterLoad.PSApi
{
    /// <summary>
    /// The callback that reports the render progress to PDN.
    /// </summary>
    /// <param name="done">The amount of work done.</param>
    /// <param name="total">The total amout of work.</param>
    internal delegate void ProgressFunc(int done, int total);
}
