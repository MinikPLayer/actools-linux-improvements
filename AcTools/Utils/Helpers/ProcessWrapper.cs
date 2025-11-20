using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace AcTools.Utils.Helpers {
    public class ProcessWrapper {
        public int ExitCode { get; private set; }

        private readonly Process _inner;
        private bool _exited, _signaled, _raisedOnExited, _watchForExit;
        private Process? _processHandle;

        public ProcessWrapper(Process inner) {
            _inner = inner;

            _processHandle = null;
            _watchForExit = false;
        }
        void RaiseOnExited() {
            if (!_raisedOnExited) {
                lock (this) {
                    if (!_raisedOnExited) {
                        _raisedOnExited = true;
                    }
                }
            }
        }

        public bool HasExitedSafe {
            get {
                // if (!_exited) {
                //     IntPtr handle = IntPtr.Zero;
                //     try {
                //         handle = GetProcessHandle(Kernel32.ProcessAccessFlags.QueryLimitedInformation | Kernel32.ProcessAccessFlags.Synchronize, false);
                //         if (handle == IntPtr.Zero || handle == new IntPtr(-1)) {
                //             _exited = true;
                //         } else {
                //             int exitCode;
                //
                //             // Although this is the wrong way to check whether the process has exited,
                //             // it was historically the way we checked for it, and a lot of code then took a dependency on
                //             // the fact that this would always be set before the pipes were closed, so they would read
                //             // the exit code out after calling ReadToEnd() or standard output or standard error. In order
                //             // to allow 259 to function as a valid exit code and to break as few people as possible that
                //             // took the ReadToEnd dependency, we check for an exit code before doing the more correct
                //             // check to see if we have been signalled.
                //             if (Kernel32.GetExitCodeProcess(handle, out exitCode) && exitCode != Kernel32.STILL_ACTIVE) {
                //                 _exited = true;
                //                 ExitCode = exitCode;
                //             } else {
                //
                //                 // The best check for exit is that the kernel process object handle is invalid,
                //                 // or that it is valid and signaled.  Checking if the exit code != STILL_ACTIVE
                //                 // does not guarantee the process is closed,
                //                 // since some process could return an actual STILL_ACTIVE exit code (259).
                //                 if (!_signaled) // if we just came from WaitForExit, don't repeat
                //                 {
                //                     ProcessWaitHandle wh = null;
                //                     try {
                //                         wh = new ProcessWaitHandle(handle);
                //                         _signaled = wh.WaitOne(0, false);
                //                     } finally {
                //                         wh?.Close();
                //                     }
                //                 }
                //                 if (_signaled) {
                //                     if (!Kernel32.GetExitCodeProcess(handle, out exitCode)) {
                //                         throw new Win32Exception();
                //                     }
                //
                //                     _exited = true;
                //                     ExitCode = exitCode;
                //                 }
                //             }
                //         }
                //     } finally {
                //         ReleaseProcessHandle(handle);
                //     }
                //
                //     if (_exited) {
                //         RaiseOnExited();
                //     }
                // }
                if (!_exited)
                {
                    _exited = _inner.HasExited;
                    ExitCode = _inner.ExitCode;
                }

                return _exited;
            }
        }

        public bool WaitForExitSafe(int milliseconds) {
            var handle = IntPtr.Zero;
            // bool exited;
            // ProcessWaitHandle processWaitHandle = null;
            //
            // try {
            //     handle = GetProcessHandle(Kernel32.ProcessAccessFlags.Synchronize, false);
            //     if (handle == IntPtr.Zero || handle == new IntPtr(-1)) {
            //         exited = true;
            //     } else {
            //         processWaitHandle = new ProcessWaitHandle(handle);
            //         if (processWaitHandle.WaitOne(milliseconds, false)) {
            //             exited = true;
            //             _signaled = true;
            //         } else {
            //             exited = false;
            //             _signaled = false;
            //         }
            //     }
            // } finally {
            //     processWaitHandle?.Close();
            //     ReleaseProcessHandle(handle);
            // }
            var exited = _inner.WaitForExit(milliseconds);

            if (exited && _watchForExit) {
                RaiseOnExited();
            }

            return exited;
        }
    }
}