using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;

namespace Koos__OBD_Communicator.SensorControls
{
    interface SensorControl
    {
        ListBoxItem getSensorItem();
        void setSensorValue(double newValue);
        void setVisibility(bool visible);
    }
}
