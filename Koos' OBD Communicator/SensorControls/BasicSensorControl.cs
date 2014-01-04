using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;

namespace Koos__OBD_Communicator.SensorControls
{
    class BasicSensorControl : SensorControl
    {
        private TextBlock sensorDescription;
        private TextBlock sensorValue;
        private StackPanel sensorStack;
        private ListBoxItem sensorItem;

        public BasicSensorControl(string description)
        {
            sensorDescription = new TextBlock() { FontSize = 20, Text = description };
            sensorValue = new TextBlock() { Text = "" };

            sensorStack = new StackPanel() { Orientation = Orientation.Vertical };

            sensorStack.Children.Add(sensorDescription);
            sensorStack.Children.Add(sensorValue);

            sensorItem =  new ListBoxItem()
            {
                Content = sensorStack,
                Visibility = System.Windows.Visibility.Collapsed
            };
        }

        public void setSensorValue(double value)
        {
            this.sensorValue.Text = value.ToString();
        }

        public ListBoxItem getSensorItem()
        {
            return this.sensorItem;
        }

        public void setVisibility(bool visible)
        {
            this.sensorItem.Visibility = visible ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        }
    }
}
