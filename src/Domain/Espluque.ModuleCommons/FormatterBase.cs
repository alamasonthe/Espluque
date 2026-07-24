namespace Espluque.ModuleCommons
{
    public abstract class FormatterBase
    {
        public List<KeyValuePair<string, string>>  Format(List<KeyValuePair<string, string>> list)
        {
            if (list is null || list.Count == 0) return new();

            List < KeyValuePair<string, string> > formattedList = [];

            foreach (var item in list)
            {
                var formattedItem = Format(item);

                if (formattedItem != null)
                {
                    formattedList.Add(formattedItem.Value);
                }
            }

            return formattedList;
        }

        public abstract KeyValuePair<string, string>?  Format(KeyValuePair<string, string> item);
    }
}
