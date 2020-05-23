using System;
using System.ComponentModel;

namespace whr_wpf.Util
{
	/// <summary>
	/// プロパティ変更イベントの受け取りモジュール
	/// </summary>
	/// <see cref="https://qiita.com/nossey/items/7c415799bc6fda45f94e">参考</see>
	public class PropertyChangedEventListener : IDisposable
	{
		INotifyPropertyChanged Source;
		PropertyChangedEventHandler Handler;

		public PropertyChangedEventListener(INotifyPropertyChanged source, PropertyChangedEventHandler handler)
		{
			Source = source;
			Handler = handler;
			Source.PropertyChanged += Handler;
		}

		public void Dispose()
		{
			if (Source != null && Handler != null)
				Source.PropertyChanged -= Handler;
		}
	}
}
