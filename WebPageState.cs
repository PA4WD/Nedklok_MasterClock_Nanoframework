using System;

namespace Nedklok_MasterClock_Nanoframework
{
    internal class WebPageState
    {
        public DateTime CurrentTime { get; set; }
        public ClockState CurrentState { get; set; }
    }
}
