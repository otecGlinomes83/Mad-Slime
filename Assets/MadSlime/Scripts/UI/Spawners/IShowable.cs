using System;

namespace UI
{
    public interface IShowable
    {
        event Action Closed;

        void Show();

        void Hide();
    }
}
