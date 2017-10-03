/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2017 Nicholas Hayes
// 
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace PSFilterLoad.PSApi
{
    /// <summary>
    /// Represents a read-only collection of keys and values.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
    /// <seealso cref="System.Collections.Generic.IDictionary{TKey, TValue}"/>
    [DebuggerDisplay("Count = {Count}")]
    [Serializable]
    public sealed class ReadOnlyDictionary<TKey, TValue> : IDictionary<TKey, TValue>  
    {
        private readonly IDictionary<TKey, TValue> dictionary;
        [NonSerialized]
        private KeyCollection keys;
        [NonSerialized]
        private ValueCollection values;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReadOnlyDictionary{TKey, TValue}"/> class.
        /// </summary>
        /// <param name="dictionary">The dictionary to wrap.</param>
        /// <exception cref="ArgumentNullException"><paramref name="dictionary"/> is null.</exception>
        public ReadOnlyDictionary(IDictionary<TKey, TValue> dictionary)
        {
            if (dictionary == null)
            {
                throw new ArgumentNullException("dictionary");
            }

            this.dictionary = dictionary;
            this.keys = null;
            this.values = null;
        }

        /// <summary>
        /// Gets the number of elements contained in the <see cref="ReadOnlyDictionary{TKey, TValue}"/>.
        /// </summary>
        public int Count
        {
            get
            {
                return this.dictionary.Count;
            }
        }

        /// <summary>
        /// Gets a collection containing the keys in the dictionary.
        /// </summary>
        public KeyCollection Keys
        {
            get
            {
                if (this.keys == null)
                {
                    this.keys = new KeyCollection(this.dictionary.Keys);
                }

                return this.keys;
            }
        }

        /// <summary>
        /// Gets a collection containing the values in the dictionary.
        /// </summary>
        public ValueCollection Values
        {
            get
            {
                if (this.values == null)
                {
                    this.values = new ValueCollection(this.dictionary.Values);
                }

                return this.values;
            }
        }

        /// <summary>
        /// Gets the value with the specified key.
        /// </summary>
        /// <value>
        /// The value.
        /// </value>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public TValue this[TKey key]
        {
            get
            {
                return this.dictionary[key];
            }
        }

        /// <summary>
        /// Determines whether the <see cref="ReadOnlyDictionary{TKey, TValue}"/> contains an element with the specified key.
        /// </summary>
        /// <param name="key">The key to locate in the <see cref="ReadOnlyDictionary{TKey, TValue}"/>.</param>
        /// <returns>
        /// true if the <see cref="ReadOnlyDictionary{TKey, TValue}" /> contains an element with the key; otherwise, false.
        /// </returns>
        public bool ContainsKey(TKey key)
        {
            return this.dictionary.ContainsKey(key);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="ReadOnlyDictionary{TKey, TValue}"/>.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Collections.Generic.IEnumerator`1" /> that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return this.dictionary.GetEnumerator();
        }

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key whose value to get.</param>
        /// <param name="value">
        /// When this method returns, the value associated with the specified key, if the key is found;
        /// otherwise, the default value for the type of the <paramref name="value" /> parameter. 
        /// This parameter is passed uninitialized.
        /// </param>
        /// <returns>
        /// true if the object that implements <see cref="ReadOnlyDictionary{TKey, TValue}"/> contains an element with the specified key; otherwise, false.
        /// </returns>
        public bool TryGetValue(TKey key, out TValue value)
        {
            return this.dictionary.TryGetValue(key, out value);
        }

        private static void ThrowNotSupportedException()
        {
            throw new NotSupportedException("The dictionary is read only.");
        }

        #region ICollection<KeyValuePair<TKey, TValue>> Methods
        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly
        {
            get
            {
                return true;
            }
        }

        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
        {
            ThrowNotSupportedException();
        }

        void ICollection<KeyValuePair<TKey, TValue>>.Clear()
        {
            ThrowNotSupportedException();
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
        {
            return this.dictionary.Contains(item);
        }

        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            this.dictionary.CopyTo(array, arrayIndex);
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
        {
            ThrowNotSupportedException();
            return false;
        } 
        #endregion

        #region IDictionary<TKey, TValue> Methods
        ICollection<TKey> IDictionary<TKey, TValue>.Keys
        {
            get
            {
                return new KeyCollection(this.dictionary.Keys);
            }
        }

        ICollection<TValue> IDictionary<TKey, TValue>.Values
        {
            get
            {
                return new ValueCollection(this.dictionary.Values);
            }
        }

        int ICollection<KeyValuePair<TKey, TValue>>.Count
        {
            get
            {
                return this.dictionary.Count;
            }
        }

        TValue IDictionary<TKey, TValue>.this[TKey key]
        {
            get
            {
                return this.dictionary[key];
            }
            set
            {
                ThrowNotSupportedException();
            }
        }

        void IDictionary<TKey, TValue>.Add(TKey key, TValue value)
        {
            ThrowNotSupportedException();
        }

        bool IDictionary<TKey, TValue>.ContainsKey(TKey key)
        {
            return this.dictionary.ContainsKey(key);
        }

        bool IDictionary<TKey, TValue>.Remove(TKey key)
        {
            ThrowNotSupportedException();
            return false;
        }

        bool IDictionary<TKey, TValue>.TryGetValue(TKey key, out TValue value)
        {
            return this.dictionary.TryGetValue(key, out value);
        } 
        #endregion

        #region IEnumerator Methods
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.dictionary.GetEnumerator();
        } 
        #endregion

        /// <summary>
        /// Represents a read-only collection of keys in the <see cref="ReadOnlyDictionary{TKey, TValue}"/>
        /// </summary>
        /// <seealso cref="System.Collections.Generic.ICollection{T}" />
        [DebuggerDisplay("Count = {Count}")]
        [Serializable]
        public sealed class KeyCollection : ICollection<TKey>
        {
            private readonly ICollection<TKey> items;

            public KeyCollection(ICollection<TKey> items)
            {
                this.items = items;
            }

            /// <summary>
            /// Gets the number of elements contained in the collection.
            /// </summary>
            public int Count
            {
                get
                {
                    return this.items.Count;
                }
            }

            /// <summary>
            /// Determines whether the collection contains a specific value.
            /// </summary>
            /// <param name="item">The object to locate in the collection.</param>
            /// <returns>
            /// true if <paramref name="item" /> is found in the collection; otherwise, false.
            /// </returns>
            public bool Contains(TKey item)
            {
                return this.items.Contains(item);
            }

            /// <summary>
            /// Copies the elements of the collection to an <see cref="T:System.Array"/>, starting at a particular <see cref="T:System.Array"/> index.
            /// </summary>
            /// <param name="array">
            /// The one-dimensional <see cref="T:System.Array" /> that is the destination of the elements copied from collection.
            /// The <see cref="T:System.Array" /> must have zero-based indexing.
            /// </param>
            /// <param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param>
            public void CopyTo(TKey[] array, int arrayIndex)
            {
                this.items.CopyTo(array, arrayIndex);
            }

            /// <summary>
            /// Returns an enumerator that iterates through the collection.
            /// </summary>
            /// <returns>
            /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
            /// </returns>
            public IEnumerator<TKey> GetEnumerator()
            {
                return this.items.GetEnumerator();
            }

            bool ICollection<TKey>.IsReadOnly
            {
                get
                {
                    return true;
                }
            }

            void ICollection<TKey>.Add(TKey item)
            {
                ThrowNotSupportedException();
            }

            void ICollection<TKey>.Clear()
            {
                ThrowNotSupportedException();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.items.GetEnumerator();
            }

            bool ICollection<TKey>.Remove(TKey item)
            {
                ThrowNotSupportedException();
                return false;
            }
        }

        /// <summary>
        /// Represents a read-only collection of values in the <see cref="ReadOnlyDictionary{TKey, TValue}"/>
        /// </summary>
        /// <seealso cref="System.Collections.Generic.ICollection{T}" />
        [DebuggerDisplay("Count = {Count}")]
        [Serializable]
        public sealed class ValueCollection : ICollection<TValue>
        {
            private readonly ICollection<TValue> items;

            public ValueCollection(ICollection<TValue> items)
            {
                this.items = items;
            }

            /// <summary>
            /// Gets the number of elements contained in the collection.
            /// </summary>
            public int Count
            {
                get
                {
                    return this.items.Count;
                }
            }

            /// <summary>
            /// Determines whether the collection contains a specific value.
            /// </summary>
            /// <param name="item">The object to locate in the collection.</param>
            /// <returns>
            /// true if <paramref name="item" /> is found in the collection; otherwise, false.
            /// </returns>
            public bool Contains(TValue item)
            {
                return this.items.Contains(item);
            }

            /// <summary>
            /// Copies the elements of the collection to an <see cref="T:System.Array"/>, starting at a particular <see cref="T:System.Array"/> index.
            /// </summary>
            /// <param name="array">
            /// The one-dimensional <see cref="T:System.Array" /> that is the destination of the elements copied from collection. 
            /// The <see cref="T:System.Array" /> must have zero-based indexing.
            /// </param>
            /// <param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param>
            public void CopyTo(TValue[] array, int arrayIndex)
            {
                this.items.CopyTo(array, arrayIndex);
            }

            /// <summary>
            /// Returns an enumerator that iterates through the collection.
            /// </summary>
            /// <returns>
            /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
            /// </returns>
            public IEnumerator<TValue> GetEnumerator()
            {
                return this.items.GetEnumerator();
            }

            bool ICollection<TValue>.IsReadOnly
            {
                get
                {
                    return true;
                }
            }

            void ICollection<TValue>.Add(TValue item)
            {
                ThrowNotSupportedException();
            }

            void ICollection<TValue>.Clear()
            {
                ThrowNotSupportedException();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.items.GetEnumerator();
            }

            bool ICollection<TValue>.Remove(TValue item)
            {
                ThrowNotSupportedException();
                return false;
            }
        }
    }
}
