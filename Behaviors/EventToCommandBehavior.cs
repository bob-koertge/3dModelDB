using System.Windows.Input;

namespace MauiApp3.Behaviors
{
    public class EventToCommandBehavior : Behavior<View>
    {
        public static readonly BindableProperty EventNameProperty =
            BindableProperty.Create(nameof(EventName), typeof(string), typeof(EventToCommandBehavior));

        public static readonly BindableProperty CommandProperty =
            BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(EventToCommandBehavior));

        public static readonly BindableProperty CommandParameterProperty =
            BindableProperty.Create(nameof(CommandParameter), typeof(object), typeof(EventToCommandBehavior));

        public string EventName
        {
            get => (string)GetValue(EventNameProperty);
            set => SetValue(EventNameProperty, value);
        }

        public ICommand? Command
        {
            get => (ICommand?)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        public object? CommandParameter
        {
            get => GetValue(CommandParameterProperty);
            set => SetValue(CommandParameterProperty, value);
        }

        private Delegate? _eventHandler;

        protected override void OnAttachedTo(View bindable)
        {
            base.OnAttachedTo(bindable);

            if (string.IsNullOrWhiteSpace(EventName))
                return;

            var eventInfo = bindable.GetType().GetEvent(EventName);
            if (eventInfo == null)
                return;

            _eventHandler = CreateEventHandler(eventInfo);
            eventInfo.AddEventHandler(bindable, _eventHandler);
        }

        protected override void OnDetachingFrom(View bindable)
        {
            base.OnDetachingFrom(bindable);

            if (string.IsNullOrWhiteSpace(EventName) || _eventHandler == null)
                return;

            var eventInfo = bindable.GetType().GetEvent(EventName);
            if (eventInfo != null)
            {
                eventInfo.RemoveEventHandler(bindable, _eventHandler);
            }

            _eventHandler = null;
        }

        private Delegate CreateEventHandler(System.Reflection.EventInfo eventInfo)
        {
            var eventHandlerType = eventInfo.EventHandlerType;
            if (eventHandlerType == null)
                throw new InvalidOperationException($"Event {EventName} has no event handler type");

            return eventHandlerType.Name switch
            {
                "EventHandler" => new EventHandler((sender, args) => OnEventRaised()),
                "EventHandler`1" => CreateGenericEventHandler(eventHandlerType),
                _ => throw new NotSupportedException($"Event handler type {eventHandlerType.Name} is not supported")
            };
        }

        private Delegate CreateGenericEventHandler(Type eventHandlerType)
        {
            var method = GetType().GetMethod(nameof(OnEventRaisedGeneric), 
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            
            if (method == null)
                throw new InvalidOperationException("OnEventRaisedGeneric method not found");

            return Delegate.CreateDelegate(eventHandlerType, this, method);
        }

        private void OnEventRaisedGeneric(object? sender, object? args)
        {
            OnEventRaised();
        }

        private void OnEventRaised()
        {
            if (Command?.CanExecute(CommandParameter) == true)
            {
                Command.Execute(CommandParameter);
            }
        }
    }
}
