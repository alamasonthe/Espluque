using Espluque.Contracts.Interfaces;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Espluquer.Entities
{
    public class ConceptDto : INotifyPropertyChanged
    {
        private int? _id;
        private List<ConceptDto> _parents = [];
        private List<ConceptDto> _children = [];
        private List<IThesaurusTerm> _terms = [];

        public event PropertyChangedEventHandler? PropertyChanged;

        public int? Id
        {
            get => _id;
            set => SetValue(ref _id, value);
        }

        public string? Term
        {
            get
            {
                return Terms
                    .FirstOrDefault(term => term.IsPreferred)?
                    .Term;
            }
            set
            {
                IThesaurusTerm? preferredTerm = Terms.FirstOrDefault(term => term.IsPreferred);

                if (preferredTerm is null)
                {
                    return;
                }

                if (preferredTerm.Term == value)
                {
                    return;
                }

                preferredTerm.Term = value;
                preferredTerm.NormalizedTerm = value;

                NotifyPropertyChanged(nameof(Term));
            }
        }

        public List<ConceptDto> Parents
        {
            get => _parents;
            set => SetValue(ref _parents, value);
        }

        public List<ConceptDto> Children
        {
            get => _children;
            set => SetValue(ref _children, value);
        }

        public List<IThesaurusTerm> Terms
        {
            get => _terms;
            set
            {
                if (SetValue(ref _terms, value))
                {
                    NotifyPropertyChanged(nameof(Term));
                }
            }
        }

        public void NotifyTermChanged()
        {
            NotifyPropertyChanged(nameof(Term));
        }

        private void NotifyPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool SetValue<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return false;
            }

            field = value;
            NotifyPropertyChanged(propertyName);
            return true;
        }
    }
}