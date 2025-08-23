using CommunityToolkit.Mvvm.Messaging.Messages;
using ReportesDePaqueteria.MVVM.Models;

namespace ReportesDePaqueteria.MVVM.Messaging
{
    public sealed class IncidentSavedMessage : ValueChangedMessage<IncidentModel>
    {
        public IncidentSavedMessage(IncidentModel value) : base(value) { }
    }

    public sealed class IncidentDeletedMessage : ValueChangedMessage<int>
    {
        public IncidentDeletedMessage(int id) : base(id) { }
    }
}
