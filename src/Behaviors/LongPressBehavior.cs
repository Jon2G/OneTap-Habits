using System.Windows.Input;

namespace OneTapHabits.Behaviors;

public sealed class LongPressBehavior : Behavior<View>
{
	public static readonly BindableProperty CommandProperty =
		BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(LongPressBehavior));

	public static readonly BindableProperty TapCommandProperty =
		BindableProperty.Create(nameof(TapCommand), typeof(ICommand), typeof(LongPressBehavior));

	public static readonly BindableProperty CommandParameterProperty =
		BindableProperty.Create(nameof(CommandParameter), typeof(object), typeof(LongPressBehavior));

	public static readonly BindableProperty DurationProperty =
		BindableProperty.Create(nameof(Duration), typeof(int), typeof(LongPressBehavior), 500);

	public ICommand? Command
	{
		get => (ICommand?)GetValue(CommandProperty);
		set => SetValue(CommandProperty, value);
	}

	public ICommand? TapCommand
	{
		get => (ICommand?)GetValue(TapCommandProperty);
		set => SetValue(TapCommandProperty, value);
	}

	public object? CommandParameter
	{
		get => GetValue(CommandParameterProperty);
		set => SetValue(CommandParameterProperty, value);
	}

	public int Duration
	{
		get => (int)GetValue(DurationProperty);
		set => SetValue(DurationProperty, value);
	}

	private CancellationTokenSource? _pressCts;
	private bool _longPressFired;
	private PointerGestureRecognizer? _pointer;

	protected override void OnAttachedTo(View bindable)
	{
		base.OnAttachedTo(bindable);
		_pointer = new PointerGestureRecognizer();
		_pointer.PointerPressed += OnPointerPressed;
		_pointer.PointerReleased += OnPointerReleased;
		_pointer.PointerExited += OnPointerCancelled;
		bindable.GestureRecognizers.Add(_pointer);
	}

	protected override void OnDetachingFrom(View bindable)
	{
		if (_pointer is not null)
		{
			_pointer.PointerPressed -= OnPointerPressed;
			_pointer.PointerReleased -= OnPointerReleased;
			_pointer.PointerExited -= OnPointerCancelled;
			bindable.GestureRecognizers.Remove(_pointer);
			_pointer = null;
		}

		CancelPress();
		base.OnDetachingFrom(bindable);
	}

	private void OnPointerPressed(object? sender, PointerEventArgs e)
	{
		CancelPress();
		_longPressFired = false;
		_pressCts = new CancellationTokenSource();
		var token = _pressCts.Token;
		_ = WaitForLongPressAsync(token);
	}

	private async Task WaitForLongPressAsync(CancellationToken token)
	{
		try
		{
			await Task.Delay(Duration, token);
			_longPressFired = true;
			Execute(Command);
		}
		catch (TaskCanceledException)
		{
			// Short press; release handler may fire tap.
		}
	}

	private void OnPointerReleased(object? sender, PointerEventArgs e)
	{
		var wasLongPress = _longPressFired;
		CancelPress();

		if (!wasLongPress)
		{
			Execute(TapCommand);
		}
	}

	private void OnPointerCancelled(object? sender, PointerEventArgs e)
	{
		CancelPress();
		_longPressFired = false;
	}

	private void CancelPress()
	{
		_pressCts?.Cancel();
		_pressCts?.Dispose();
		_pressCts = null;
	}

	private void Execute(ICommand? command)
	{
		var parameter = CommandParameter;
		if (command?.CanExecute(parameter) == true)
		{
			command.Execute(parameter);
		}
	}
}
