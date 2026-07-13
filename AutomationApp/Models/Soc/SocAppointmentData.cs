using System;

namespace AutomationApp.Models.Soc
{
    public class SocAppointmentData : SocEmployeeData
    {
        public string DataAgendamento { get; set; } = string.Empty; 
        public string HoraAgendamento { get; set; } = string.Empty;
    }
}