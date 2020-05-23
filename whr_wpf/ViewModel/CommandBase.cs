using System;
using System.Windows.Input;

namespace whr_wpf.ViewModel
{
	/// <summary>
	/// コマンドのベース
	/// </summary>
	public abstract class CommandBase : ICommand
	{
		public event EventHandler CanExecuteChanged
		{
			add { CommandManager.RequerySuggested += value; }
			remove { CommandManager.RequerySuggested -= value; }
		}

		/// <summary>
		/// コマンドが実行できるか
		/// </summary>
		/// <param name="parameter"></param>
		/// <returns></returns>
		public abstract bool CanExecute(object parameter);

		/// <summary>
		/// コマンド実行
		/// </summary>
		/// <param name="parameter"></param>
		public abstract void Execute(object parameter);
	}
}
