using UnityEngine;
using System.Collections;

using System;
using System.Collections.Generic;
using System.Linq;
namespace doru
{
    public class Timer
    {
        public int _Ticks = Environment.TickCount;
        public int oldtime;
        public int fpstimes;
        public double totalfps;
        public double GetFps()
        {
            if (fpstimes > 0)
            {
                double fps = (totalfps / fpstimes);
                fpstimes = 0;
                totalfps = 0;
                if (fps == double.PositiveInfinity) return 0;
                return fps;
            }
            else return 0;
        }

        public int miliseconds;
        public void Update()
        {


            miliseconds = Environment.TickCount - _Ticks;
            _MilisecondsElapsed = miliseconds - oldtime;
            if (_MilisecondsElapsed > 0)
            {
                oldtime = miliseconds;
                fpstimes++;
                totalfps += Time.timeScale / Time.deltaTime;
                UpdateAction2s();
            }
        }
        private void UpdateAction2s()
        {
            CA select = null;
            lock (_List)
                foreach (var _CA in _List)
                {
                    _CA._Miliseconds -= _MilisecondsElapsed;
                    if (_CA._Miliseconds < 0 && (_CA.func == null || _CA.func()) && (select == null || select._Miliseconds > _CA._Miliseconds))
                    {
                        select = _CA;
                    }
                }
            if (select != null)
            {
                _List.Remove(select);
                //try
                {
                    select._Action2();
                }
                //catch (Exception e) { Debug.LogError("Timer:" + e.Message + "\r\n\r\n" + select.stacktrace + "\r\n\r\n"); }
            }
        }
        public int _MilisecondsElapsed = 0;
        public double _SecodsElapsed { get { return _MilisecondsElapsed / (double)1000; } }
        public int _oldTime { get { return miliseconds - _MilisecondsElapsed; } }

        public bool TimeElapsed(int _Milisecconds)
        {
            if (_MilisecondsElapsed > _Milisecconds || _Milisecconds == 0) return true;
            if (miliseconds % _Milisecconds < _oldTime % _Milisecconds)
                return true;
            return false;
        } //if seconds elapsed from last Update() call this function will be called
        public void AddMethod(Action _Action2){AddMethod(-1, _Action2, null,true);}
        public void AddMethod(Func<bool> func, Action _Action2) { AddMethod(-1, _Action2, func,true); }
        public void AddMethod(int _Miliseconds, Action _Action2) { AddMethod(_Miliseconds, _Action2, null,true); }
        public void AddMethod(int _Miliseconds, Action _Action2, Func<bool> func,bool nw)
        {
            CA ca = null;
            if (!nw)
                ca = _List.FirstOrDefault(a => a._Action2 == _Action2);
            if (ca == null)
            {
                ca = new CA();
                lock (_List)
                    _List.Add(ca);
            }
            ca.stacktrace = UnityEngine.StackTraceUtility.ExtractStackTrace();
            ca._Action2 = _Action2;
            ca._Miliseconds = _Miliseconds;
            ca.func = func;
        }
        public void Clear()
        {
            Debug.Log("Timer Clear");
            lock (_List)
                _List.Clear();
        }

        List<CA> _List = new List<CA>();
        class CA
        {
            public string stacktrace;
            public int _Miliseconds;
            public Func<bool> func;
            public Action _Action2;
        }

    }
}