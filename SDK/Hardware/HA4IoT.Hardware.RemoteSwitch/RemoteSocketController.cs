﻿using System;
using System.Collections.Generic;
using HA4IoT.Contracts.Core;
using HA4IoT.Contracts.Hardware;

namespace HA4IoT.Hardware.RemoteSwitch
{
    public class RemoteSocketController : IBinaryOutputController
    {
        private readonly object _syncRoot = new object();

        private readonly Dictionary<int, RemoteSocketOutputPort> _ports = new Dictionary<int, RemoteSocketOutputPort>();
        private readonly LPD433MHzSignalSender _sender;

        public RemoteSocketController(DeviceId id, LPD433MHzSignalSender sender, IHomeAutomationTimer timer)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));
            if (sender == null) throw new ArgumentNullException(nameof(sender));
            if (timer == null) throw new ArgumentNullException(nameof(timer));

            Id = id;
            _sender = sender;

            // Ensure that the state of the remote switch is restored if the original remote is used
            // or the switch has been removed from the socket and plugged in at another place.
            timer.Every(TimeSpan.FromSeconds(5)).Do(RefreshStates);
        }

        public DeviceId Id { get; }

        public IBinaryOutput GetOutput(int number)
        {
            if (number < 0) throw new ArgumentOutOfRangeException(nameof(number));

            lock (_syncRoot)
            {
                RemoteSocketOutputPort output;
                if (!_ports.TryGetValue(number, out output))
                {
                    throw new InvalidOperationException("No remote switch with ID " + number + " is registered.");    
                }

                return output;
            }
        }

        public RemoteSocketController WithRemoteSocket(int id, LPD433MHzCodeSequence onCodeSequence, LPD433MHzCodeSequence offCodeSequence)
        {
            if (onCodeSequence == null) throw new ArgumentNullException(nameof(onCodeSequence));
            if (offCodeSequence == null) throw new ArgumentNullException(nameof(offCodeSequence));

            lock (_syncRoot)
            {
                var port = new RemoteSocketOutputPort(id, onCodeSequence, offCodeSequence, _sender);
                port.Write(BinaryState.Low);

                _ports.Add(id, port);
            }

            return this;
        }

        private void RefreshStates()
        {
            foreach (var port in _ports.Values)
            {
                port.Write(port.Read());
            }
        }
    }
}