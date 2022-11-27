using System;
using System.Device.Gpio;
using System.Threading;

namespace Nedklok_MasterClock_Nanoframework
{
    internal static class Clock
    {
        private static GpioPin L9110S_1A, L9110S_1B, LED;
        private static int _seconds;
        private static ClockState _clockState;
        private static Thread _clockThread;

        public static void InitClock()
        {
            var gpioController = new GpioController();
            LED = gpioController.OpenPin(2, PinMode.Output);
            L9110S_1A = gpioController.OpenPin(26, PinMode.Output);
            L9110S_1B = gpioController.OpenPin(25, PinMode.Output);

            _clockThread = new(ClockThread);
            _clockThread.Start();
        }

        public static void ChangeClockState(ClockState state)
        {
            _clockState = state;
        }

        public static void ChangeClockState(ClockState state, int seconds)
        {
            _seconds = seconds;
            _clockState = state;
        }

        public static ClockState GetClockState()
        {
            return _clockState;
        }

        private static void ClockThread()
        {
            int lastsecond = 0;
            while (true)
            {
                if (DateTime.UtcNow.Second != lastsecond)
                {
                    lastsecond = DateTime.UtcNow.Second;
                    switch (_clockState)
                    {
                        case ClockState.Running:
                            TickSecond(1);
                            break;
                        case ClockState.Stopped:
                            break;
                        case ClockState.Adding:
                            TickSecond(_seconds);
                            _clockState = ClockState.Running;
                            break;
                        case ClockState.waiting:
                            _seconds--;
                            if (_seconds == 0)
                            {
                                _clockState = ClockState.Running;
                            }

                            break;
                        default:
                            break;
                    }
                }
                Thread.Sleep(1);
            }
        }

        private static bool positive;
        private static void TickSecond(int ticks)
        {
            for (int i = ticks; i > 0; i--)
            {
                //Debug.WriteLine($"Tick = {i}");
                if (positive)
                {
                    L9110S_1A.Write(1);
                    positive = false;
                }
                else
                {
                    L9110S_1B.Write(1);
                    positive = true;
                }
                Thread.Sleep(200);

                L9110S_1A.Write(0);
                L9110S_1B.Write(0);

                LED.Toggle();

                if (i > 1)
                {
                    Thread.Sleep(200);
                }
            }
        }
    }
}
