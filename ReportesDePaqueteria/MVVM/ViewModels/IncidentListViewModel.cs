using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Maui.ApplicationModel;
using Firebase.Database.Streaming;
using ReportesDePaqueteria.MVVM.Models;
using ReportesDePaqueteria.MVVM.Views;
using ReportesDePaqueteria.MVVM.Messaging;

namespace ReportesDePaqueteria.MVVM.ViewModels
{
    public partial class IncidentListViewModel :
        ObservableRecipient,
        IRecipient<IncidentSavedMessage>,
        IRecipient<IncidentDeletedMessage>
    {
        private readonly IIncidentRepository _incidents;
        private IDisposable? _liveSub;   

        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private bool isRefreshing;
        [ObservableProperty] private string? searchText;

        public ObservableCollection<IncidentModel> Incidentes { get; } = new();
        private readonly List<IncidentModel> _all = new();

        public IncidentListViewModel(IIncidentRepository incidents)
        {
            _incidents = incidents ?? throw new ArgumentNullException(nameof(incidents));
            IsActive = true;

            PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(SearchText))
                    ApplyFilter();
            };
        }

        protected override void OnActivated()
        {
            base.OnActivated();

            _liveSub ??= _incidents.ObserveAll().Subscribe(OnFirebaseEvent, OnFirebaseError);
        }

        protected override void OnDeactivated()
        {
            base.OnDeactivated();
            _liveSub?.Dispose();
            _liveSub = null;
        }

        [RelayCommand]
        public async Task LoadAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                _all.Clear();
                Incidentes.Clear();

                var dict = await _incidents.GetAllAsync();
                foreach (var m in dict.Values.OrderByDescending(i => i.DateTime))
                {
                    m.Title ??= string.Empty;
                    m.Description ??= string.Empty;
                    m.ShipmentCode ??= string.Empty;
                    _all.Add(m);
                }

                ApplyFilter();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
            finally
            {
                IsBusy = false;
                IsRefreshing = false;
            }
        }

        [RelayCommand]
        private async Task OpenAsync(IncidentModel? item)
        {
            if (item is null || item.Id <= 0) return;
            await Shell.Current.GoToAsync($"{nameof(IncidentDetailPage)}?id={item.Id}");
        }

        private void ApplyFilter()
        {
            var q = (SearchText ?? string.Empty).Trim().ToLowerInvariant();
            var query = string.IsNullOrEmpty(q)
                ? _all.AsEnumerable()
                : _all.Where(i =>
                        (i.Title ?? "").ToLowerInvariant().Contains(q) ||
                        (i.Description ?? "").ToLowerInvariant().Contains(q) ||
                        ((i.Assignee?.Name ?? i.Assignee?.Email ?? i.AssigneeId ?? "")
                            .ToLowerInvariant()
                            .Contains(q)));

            Incidentes.Clear();
            foreach (var m in query)
                Incidentes.Add(m);
        }

        public void Receive(IncidentSavedMessage message)
        {
            var updated = message.Value;
            if (updated is null) return;

            UpsertLocal(updated);
        }

        public void Receive(IncidentDeletedMessage message)
        {
            var id = message.Value;
            RemoveLocal(id);
        }

        private void OnFirebaseEvent(FirebaseEvent<IncidentModel> ev)
        {
            try
            {
                if (ev is null) return;
                var model = ev.Object;
                if (model is null) return;

                if (model.Id <= 0 && int.TryParse(ev.Key, out var parsed))
                    model.Id = parsed;

                switch (ev.EventType)
                {
                    case FirebaseEventType.InsertOrUpdate:
                        UpsertLocal(model);
                        break;

                    case FirebaseEventType.Delete:
                        var delId = model.Id > 0 ? model.Id
                                  : (int.TryParse(ev.Key, out var k) ? k : 0);
                        if (delId > 0) RemoveLocal(delId);
                        break;

                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[IncidentListVM] live event error: {ex}");
            }
        }

        private void OnFirebaseError(Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[IncidentListVM] live stream error: {ex}");
        }

        private void UpsertLocal(IncidentModel updated)
        {
            var idx = _all.FindIndex(x => x.Id == updated.Id);
            if (idx >= 0) _all[idx] = updated;
            else _all.Insert(0, updated);

            _all.Sort((a, b) => b.DateTime.CompareTo(a.DateTime));

            MainThread.BeginInvokeOnMainThread(ApplyFilter);
        }

        private void RemoveLocal(int id)
        {
            _all.RemoveAll(x => x.Id == id);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                var item = Incidentes.FirstOrDefault(x => x.Id == id);
                if (item != null) Incidentes.Remove(item);
            });
        }
    }
}
