using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace liXMaiLips.UI;

// Допоміжна модель для динамічного параметра (синій блок)
public class ParameterItem : INotifyPropertyChanged
{
    private string _name;
    public string Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(); }
    }

    public ObservableCollection<string> Values { get; set; } = new();

    private bool _isAddingValue;
    public bool IsAddingValue
    {
        get => _isAddingValue;
        set { _isAddingValue = value; OnPropertyChanged(); }
    }

    private string _newValueInput;
    public string NewValueInput
    {
        get => _newValueInput;
        set { _newValueInput = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
