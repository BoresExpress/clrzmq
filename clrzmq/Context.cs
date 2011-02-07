﻿/*

    Copyright (c) 2010 Jeffrey Dik <s450r1@gmail.com>
    Copyright (c) 2010 Martin Sustrik <sustrik@250bpm.com>
    Copyright (c) 2010 Michael Compton <michael.compton@littleedge.co.uk>

    This file is part of clrzmq2.

    clrzmq2 is free software; you can redistribute it and/or modify it under
    the terms of the Lesser GNU General Public License as published by
    the Free Software Foundation; either version 3 of the License, or
    (at your option) any later version.

    clrzmq2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    Lesser GNU General Public License for more details.

    You should have received a copy of the Lesser GNU General Public License
    along with this program. If not, see <http://www.gnu.org/licenses/>.

*/

using System;
using System.Runtime.InteropServices;

namespace ZMQ {
    /// <summary>
    /// ZMQ Context
    /// </summary>
    public class Context : IDisposable {
        private IntPtr ptr;

        /// <summary>
        /// Create ZMQ Context
        /// </summary>
        /// <param name="io_threads">Thread pool size</param>
        public Context(int io_threads) {
            ptr = C.zmq_init(io_threads);
            if (ptr == IntPtr.Zero)
                throw new Exception();
        }

        ~Context() {
            Dispose(false);
        }

        /// <summary>
        /// Creates a new ZMQ socket
        /// </summary>
        /// <param name="type">Type of socket to be created</param>
        /// <returns>A new ZMQ socket</returns>
        public Socket Socket(SocketType type) {
            return new Socket(CreateSocketPtr(type));
        }

        internal IntPtr CreateSocketPtr(SocketType type) {
            IntPtr socket_ptr = C.zmq_socket(ptr, (int)type);
            if (ptr == IntPtr.Zero)
                throw new Exception();
            return socket_ptr;
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) {
            if (ptr != IntPtr.Zero) {
                int rc = C.zmq_term(ptr);
                ptr = IntPtr.Zero;
                if (rc != 0)
                    throw new Exception();
            }
        }

        /// <summary>
        /// Polls the supplied items for events
        /// </summary>
        /// <param name="items">Items to Poll</param>
        /// <param name="timeout">Timeout</param>
        /// <returns>Number of Poll items with events</returns>
        public int Poll(PollItem[] items, long timeout) {
            return Poller(items, timeout);
        }

        /// <summary>
        /// Polls the supplied items for events, with immediate timeout
        /// </summary>
        /// <param name="items">Items to Poll</param>
        /// <returns>Number of Poll items with events</returns>
        public int Poll(PollItem[] items) {
            return Poll(items, -1);
        }

        /// <summary>
        /// Polls the supplied items for events. Static method
        /// </summary>
        /// <param name="items">Items to Poll</param>
        /// <param name="timeout">Timeout</param>
        /// <returns>Number of Poll items with events</returns>
        public static int Poller(PollItem[] items, long timeout) {
            int sizeOfZPL = Marshal.SizeOf(typeof(ZMQPollItem));
            IntPtr offset = IntPtr.Zero;
            int rc = 0;
            using (DisposableIntPtr itemList = new DisposableIntPtr(sizeOfZPL * items.Length)) {
                offset = itemList.Ptr;
                foreach (PollItem item in items) {
                    Marshal.StructureToPtr(item.ZMQPollItem, offset, false);
#if x64
                    offset = new IntPtr(offset.ToInt64() + sizeOfZPL);
#else
                    offset = new IntPtr(offset.ToInt32() + sizeOfZPL);
#endif
                }
                rc = C.zmq_poll(itemList.Ptr, items.Length, timeout);
                if (rc > 0) {
                    offset = itemList.Ptr;
                    for (int index = 0; index < items.Length; index++) {
                        items[index].ZMQPollItem = (ZMQPollItem)
                            Marshal.PtrToStructure(offset, typeof(ZMQPollItem));
#if x64
                        offset = new IntPtr(offset.ToInt64() + sizeOfZPL);
#else
                        offset = new IntPtr(offset.ToInt32() + sizeOfZPL);
#endif
                    }
                    foreach (PollItem item in items) {
                        item.FireEvents();
                    }
                } else if (rc < 0) {
                    throw new Exception();
                }
            }
            return rc;
        }

        /// <summary>
        /// Polls the supplied items for events, with immediate timeout. Static method
        /// </summary>
        /// <param name="items">Items to Poll</param>
        /// <returns>Number of Poll items with events</returns>
        public static int Poller(PollItem[] items) {
            return Poller(items, -1);
        }
    }
}