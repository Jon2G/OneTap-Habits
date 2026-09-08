namespace OneTapHabits.Behaviors;

using Microsoft.Maui.Controls;

public class LongPressBehavior : Behavior<View>
{
	public static readonly BindableProperty CommandProperty =
		BindableProperty.Create(nameof(Command), typeof(System.Windows.Input.ICommand), typeof(LongPressBehavior));

	public static readonly BindableProperty CommandParameterProperty =
		BindableProperty.Create(nameof(CommandParameter), typeof(object), typeof(LongPressBehavior));

	public static readonly BindableProperty DurationProperty =
		BindableProperty.Create(nameof(Duration), typeof(int), typeof(LongPressBehavior), 500);

	public System.Windows.Input.ICommand? Command
	{
		get => (System.Windows.Input.ICommand?)GetValue(CommandProperty);
		set => SetValue(CommandProperty, value);
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
	private PointerGestureRecognizer? _pointer;

	protected override void OnAttachedTo(View bindable)
	{
		base.OnAttachedTo(bindable);
		_pointer = new PointerGestureRecognizer();
		_pointer.PointerPressed += OnPointerPressed;
		_pointer.PointerReleased += OnPointerReleased;
		_pointer.PointerExited += OnPointerReleased;
		bindable.GestureRecognizers.Add(_pointer);
	}

	protected override void OnDetachingFrom(View bindable)
	{
		if (_pointer is not null)
		{
			_pointer.PointerPressed -= OnPointerPressed;
			_pointer.PointerReleased -= OnPointerReleased;
			_pointer.PointerExited -= OnPointerReleased;
			bindable.GestureRecognizers.Remove(_pointer);
			_pointer = null;
		}

		CancelPress();
		base.OnDetachingFrom(bindable);
	}

	private void OnPointerPressed(object? sender, PointerEventArgs e)
	{
		CancelPress();
		_pressCts = new CancellationTokenSource();
		var token = _pressCts.Token;
		_ = Task.Run(async () =>
		{
			try
			{
				await Task.Delay(Duration, token);
				if (token.IsCancellationRequested)
				{
					return;
				}

				MainThread.BeginInvokeOnMainThread(() =>
				{
					if (Command?.CanExecute(CommandParameter) == true)
					{
						Command.Execute(CommandParameter);
					}
				});
			}
			catch (TaskCanceledException)
			{
				// Press released before long-press threshold.
			}
		}, token);
	}

	private void OnPointerReleased(object? sender, PointerEventArgs e) => CancelPress();

	private void CancelPress()
	{
		_pressCts?.Cancel();
		_pressCts?.Dispose();
		_pressCts = null;
	}
}
