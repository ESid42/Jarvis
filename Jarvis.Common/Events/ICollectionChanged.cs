﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.Common.Events
{
	public interface ICollectionChanged
	{
		public event EventHandler<CollectionChangedEventArgs>? CollectionChanged;
	}
}
