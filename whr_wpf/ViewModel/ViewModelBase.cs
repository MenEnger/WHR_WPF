using System;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using whr_wpf.Model;

namespace whr_wpf.ViewModel
{
	/// <summary>
	/// VMのベース
	/// </summary>
	abstract public class ViewModelBase : INotifyPropertyChanged
	{
		public Window window;
		public event PropertyChangedEventHandler PropertyChanged;

		protected delegate void ExecuteDelegete();

		protected void OnPropertyChanged(string propertyName)
		{
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		/// <summary>
		/// 実行
		/// 資金消費をする場合の例外ハンドリングも含む
		/// </summary>
		/// <param name="executeDelegete">実行するメソッド</param>
		protected void ExecuteWithMoney(ExecuteDelegete executeDelegete) => ExecuteWithMoney(null, executeDelegete, false);

		/// <summary>
		/// 確認して実行
		/// 資金消費をする場合の例外ハンドリングも含む
		/// </summary>
		/// <param name="message">確認メッセージ</param>
		/// <param name="execDelegete">実行するメソッド</param>
		protected void ExecuteWithMoney(string message, ExecuteDelegete executeDelegete) => ExecuteWithMoney(message, executeDelegete, true);

		/// <summary>
		/// 確認して実行
		/// 資金消費をする場合の例外ハンドリングも含む
		/// </summary>
		/// <param name="message">確認メッセージ</param>
		/// <param name="execDelegete">実行するメソッド</param>
		/// <param name="isShowMessage">確認メッセージを表示するか</param>
		private void ExecuteWithMoney(string message, ExecuteDelegete execDelegete, bool isShowMessage)
		{
			if (isShowMessage)
			{
				MessageBoxResult constructConfirm = MessageBox.Show(message, "", MessageBoxButton.YesNo);

				if (constructConfirm == MessageBoxResult.Yes)
				{
					Execute(execDelegete);
				}
			}
			else
			{
				Execute(execDelegete);
			}

			window.Close();

			//コマンド実行と例外ハンドラ
			static void Execute(ExecuteDelegete execDelegete)
			{
				try
				{
					execDelegete();
				}
				catch (MoneyShortException)
				{
					MessageBox.Show("お金が足りません");
				}
				catch (InvalidOperationException e)
				{
					MessageBox.Show(e.Message, "エラー", MessageBoxButton.OK, MessageBoxImage.Information);
				}
			}
		}

		/// <summary>
		/// 各プロパティ情報のイベントを発火させ、UIを更新させる
		/// </summary>
		protected void InvokeAllNotify()
		{
			PropertyInfo[] infoArray = this.GetType().GetProperties();

			foreach (PropertyInfo info in infoArray)
			{
				this.OnPropertyChanged(info.Name);
			}
		}
	}
}
