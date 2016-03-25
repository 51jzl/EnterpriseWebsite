using Lucene.Net.Analysis;
using Lucene.Net.Analysis.PanGu;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using PanGu;
using PanGu.HighLight;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Victornet.Utilities;
namespace Victornet.Search
{
	public class SearchEngine : ISearchEngine
	{
		private Analyzer analyzer;
		private int mergeFactor;
		private string indexPath;
		private bool initialized = false;
		private static Highlighter highlighter = null;
		private object _lock = new object();
		private static readonly object lockObject = new object();
		private static ConcurrentDictionary<string, IndexWriter> indexWriters = new ConcurrentDictionary<string, IndexWriter>();
		private static ConcurrentDictionary<string, SearcherManager> searcherManagers = new ConcurrentDictionary<string, SearcherManager>();
		protected SearchEngine()
		{
		}
		public SearchEngine(string indexPath)
		{
			this.Initialize(indexPath, 10, null);
		}
		public SearchEngine(string indexPath, int mergeFactor, Analyzer analyzer)
		{
			this.Initialize(indexPath, mergeFactor, analyzer);
		}
		public void Initialize(string indexPath, int mergeFactor = 10, Analyzer analyzer = null)
		{
			if (!this.initialized)
			{
				lock (SearchEngine.lockObject)
				{
					if (!this.initialized)
					{
						this.indexPath = WebUtility.GetPhysicalFilePath(indexPath);
						if (mergeFactor < 10)
						{
							mergeFactor = 10;
						}
						this.mergeFactor = mergeFactor;
						if (analyzer == null)
						{
							analyzer = new PanGuAnalyzer();
						}
						this.analyzer = analyzer;
						this.InitSearcherManager();
						this.initialized = true;
					}
				}
			}
		}
		public double GetIndexSize()
		{
			double num = 0.0;
			if (this.IsIndexFileExists())
			{
				FileInfo[] files = new DirectoryInfo(this.indexPath).GetFiles("*.*");
				if (files != null && files.Length > 0)
				{
					FileInfo[] array = files;
					for (int i = 0; i < array.Length; i++)
					{
						FileInfo fileInfo = array[i];
						num += (double)fileInfo.Length;
					}
				}
			}
			return num;
		}
		public DateTime GetLastModified()
		{
			DateTime dateTime = DateTime.MinValue;
			if (this.IsIndexFileExists())
			{
				FileInfo[] files = new DirectoryInfo(this.indexPath).GetFiles("*.*");
				if (files != null && files.Length > 0)
				{
					FileInfo[] array = files;
					for (int i = 0; i < array.Length; i++)
					{
						FileInfo fileInfo = array[i];
						if (fileInfo.LastWriteTime > dateTime)
						{
							dateTime = fileInfo.LastWriteTime;
						}
					}
				}
			}
			return dateTime;
		}
		public void RebuildIndex(IEnumerable<Document> indexDocuments, bool isBeginning, bool isEndding)
		{
			lock (this._lock)
			{
				string text = Path.Combine(this.indexPath, "ReIndex");
				IndexWriter indexWriter = null;
				if (isBeginning)
				{
					if (System.IO.Directory.Exists(text))
					{
						string[] files = System.IO.Directory.GetFiles(text);
						string[] array = files;
						for (int i = 0; i < array.Length; i++)
						{
							string text2 = array[i];
							File.Delete(text2);
						}
					}
					else
					{
						System.IO.Directory.CreateDirectory(text);
					}
					Lucene.Net.Store.Directory d = FSDirectory.Open(new DirectoryInfo(text));
					indexWriter = new IndexWriter(d, this.analyzer, true, IndexWriter.MaxFieldLength.UNLIMITED);
					SearchEngine.indexWriters[text] = indexWriter;
				}
				else
				{
					indexWriter = this.GetIndexWriter(text);
				}
				if (indexDocuments != null && indexDocuments.Count<Document>() > 0)
				{
					try
					{
						foreach (Document current in indexDocuments)
						{
							indexWriter.AddDocument(current);
						}
					}
					catch (Exception innerException)
					{
						throw new ExceptionFacade(string.Format("An unexpected error occured while add documents to the index [{0}].", this.indexPath), innerException);
					}
				}
				if (isEndding)
				{
					indexWriter.Optimize();
					this.CloseIndexWriter(text);
					this.CloseSearcherManager();
					this.CloseIndexWriter();
					string[] files = System.IO.Directory.GetFiles(this.indexPath, "*.*", SearchOption.TopDirectoryOnly);
					string[] array = files;
					for (int i = 0; i < array.Length; i++)
					{
						string text2 = array[i];
						File.Delete(text2);
					}
					files = System.IO.Directory.GetFiles(text, "*.*", SearchOption.TopDirectoryOnly);
					array = files;
					for (int i = 0; i < array.Length; i++)
					{
						string text2 = array[i];
						File.Move(text2, text2.Replace("\\ReIndex", string.Empty));
					}
					System.IO.Directory.Delete(text);
					this.InitSearcherManager();
				}
			}
		}
		public void Insert(Document indexDocument, bool reopen = true)
		{
			this.Insert(new Document[]
			{
				indexDocument
			}, reopen);
		}
		public void Insert(IEnumerable<Document> indexDocuments, bool reopen = true)
		{
			lock (this._lock)
			{
				if (indexDocuments != null && indexDocuments.Count<Document>() != 0)
				{
					IndexWriter indexWriter = this.GetIndexWriter();
					try
					{
						foreach (Document current in indexDocuments)
						{
							indexWriter.AddDocument(current);
						}
					}
					catch (Exception innerException)
					{
						throw new ExceptionFacade(string.Format("An unexpected error occured while add documents to the index [{0}].", this.indexPath), innerException);
					}
					if (reopen)
					{
						this.ReopenSearcher();
					}
				}
			}
		}
		public void Delete(string id, string fieldNameOfId, bool reopen = true)
		{
			this.Delete(new string[]
			{
				id
			}, fieldNameOfId, reopen);
		}
		public void Delete(IEnumerable<string> ids, string fieldNameOfId, bool reopen = true)
		{
			lock (this._lock)
			{
				if (ids != null || ids.Count<string>() != 0)
				{
					IndexWriter indexWriter = this.GetIndexWriter();
					try
					{
						List<Term> list = new List<Term>();
						foreach (string current in ids)
						{
							Term item = new Term(fieldNameOfId, current);
							list.Add(item);
						}
						indexWriter.DeleteDocuments(list.ToArray());
					}
					catch (Exception innerException)
					{
						throw new ExceptionFacade(string.Format("An unexpected error occured while delete documents to the index [{0}].", this.indexPath), innerException);
					}
					if (reopen)
					{
						this.ReopenSearcher();
					}
				}
			}
		}
		public void Update(Document indexDocument, string id, string fieldNameOfId, bool reopen = true)
		{
			this.Update(new Document[]
			{
				indexDocument
			}, new string[]
			{
				id
			}, fieldNameOfId, reopen);
		}
		public void Update(IEnumerable<Document> indexDocuments, IEnumerable<string> ids, string fieldNameOfId, bool reopen = true)
		{
			lock (this._lock)
			{
				if (indexDocuments != null && indexDocuments.Count<Document>() != 0 && ids != null && ids.Count<string>() != 0)
				{
					try
					{
						this.Delete(ids, fieldNameOfId, false);
					}
					catch (Exception innerException)
					{
						throw new ExceptionFacade(string.Format("An unexpected error occured while delete documents to the index [{0}].", this.indexPath), innerException);
					}
					try
					{
						this.Insert(indexDocuments, false);
					}
					catch (Exception innerException)
					{
						throw new ExceptionFacade(string.Format("An unexpected error occured while add documents to the index [{0}].", this.indexPath), innerException);
					}
					if (reopen)
					{
						this.ReopenSearcher();
					}
				}
			}
		}
		public void Commit()
		{
			lock (this._lock)
			{
				IndexWriter indexWriter = this.GetIndexWriter();
				indexWriter.Commit();
			}
		}
		public void Close()
		{
			lock (this._lock)
			{
				this.CloseIndexWriter();
			}
		}
		public void Optimize()
		{
			lock (this._lock)
			{
				IndexWriter indexWriter = this.GetIndexWriter();
				indexWriter.Optimize();
			}
		}
		public PagingDataSet<Document> Search(Query searchQuery, Filter filter, Sort sort, int pageIndex, int pageSize)
		{
			PagingDataSet<Document> result;
			if (!this.IsIndexFileExists(this.indexPath))
			{
				result = new PagingDataSet<Document>(Enumerable.Empty<Document>());
			}
			else
			{
				Stopwatch stopwatch = new Stopwatch();
				stopwatch.Start();
				if (sort == null)
				{
					sort = Sort.RELEVANCE;
				}
				TopFieldCollector topFieldCollector = TopFieldCollector.Create(sort, pageIndex * pageSize, false, true, false, true);
				SearcherManager searcherManager = this.GetSearcherManager();
				IndexSearcher searcher = searcherManager.GetSearcher();
				try
				{
					searcher.Search(searchQuery, filter, topFieldCollector);
					IEnumerable<Document> entities = 
						from scoreDoc in topFieldCollector.TopDocs().ScoreDocs.Skip((pageIndex - 1) * pageSize)
						select searcher.Doc(scoreDoc.Doc);
					stopwatch.Stop();
					PagingDataSet<Document> pagingDataSet = new PagingDataSet<Document>(entities)
					{
						TotalRecords = (long)topFieldCollector.GetTotalHits(),
						PageSize = pageSize,
						PageIndex = pageIndex,
						QueryDuration = (double)stopwatch.ElapsedMilliseconds / 1000.0
					};
					result = pagingDataSet;
				}
				catch (Exception innerException)
				{
					throw new ExceptionFacade(string.Format("Index file maybe not exist under path: [{0}].", this.indexPath), innerException);
				}
				finally
				{
					searcherManager.ReleaseSearcher(searcher);
				}
			}
			return result;
		}
		public IEnumerable<Document> Search(Query searchQuery, Filter filter, Sort sort, int topNumber)
		{
			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();
			if (sort == null)
			{
				sort = Sort.RELEVANCE;
			}
			SearcherManager searcherManager = this.GetSearcherManager();
			IndexSearcher searcher = searcherManager.GetSearcher();
			IEnumerable<Document> result;
			try
			{
				TopFieldDocs topFieldDocs = searcher.Search(searchQuery, filter, topNumber, sort);
				IEnumerable<Document> enumerable = 
					from scoreDoc in topFieldDocs.ScoreDocs
					select searcher.Doc(scoreDoc.Doc);
				result = enumerable;
			}
			catch (Exception innerException)
			{
				throw new ExceptionFacade(string.Format("Index file maybe not exist under path: [{0}].", this.indexPath), innerException);
			}
			finally
			{
				searcherManager.ReleaseSearcher(searcher);
			}
			return result;
		}
		public static string Highlight(string keyWord, string content, int maxLengthOfResult)
		{
			if (SearchEngine.highlighter == null)
			{
				SearchEngine.highlighter = new Highlighter(new SimpleHTMLFormatter("<em class='tn-text-bright'>", "</em>"), new Segment());
				SearchEngine.highlighter.FragmentSize = maxLengthOfResult;
			}
			int num = 1;
			string text = null;
			if (!string.IsNullOrEmpty(content) && content.Length > num)
			{
				text = SearchEngine.highlighter.GetBestFragment(keyWord, content);
			}
			return string.IsNullOrEmpty(text) ? StringUtility.Trim(content, maxLengthOfResult) : text;
		}
		private void InitSearcherManager()
		{
			IndexWriter indexWriter = this.GetIndexWriter();
			if (indexWriter == null)
			{
				DirectoryInfo directoryInfo = new DirectoryInfo(this.indexPath);
				if (!directoryInfo.Exists)
				{
					directoryInfo.Create();
				}
				Lucene.Net.Store.Directory d = FSDirectory.Open(new DirectoryInfo(this.indexPath));
				if (IndexReader.IndexExists(FSDirectory.Open(directoryInfo)))
				{
					indexWriter = new IndexWriter(d, this.analyzer, false, IndexWriter.MaxFieldLength.UNLIMITED);
				}
				else
				{
					indexWriter = new IndexWriter(d, this.analyzer, true, IndexWriter.MaxFieldLength.UNLIMITED);
				}
				indexWriter.SetMergeFactor(this.mergeFactor);
				SearchEngine.indexWriters[this.indexPath] = indexWriter;
				SearcherManager value = new SearcherManager(indexWriter);
				SearchEngine.searcherManagers[this.indexPath] = value;
				this.initialized = true;
			}
		}
		private SearcherManager GetSearcherManager()
		{
			return this.GetSearcherManager(this.indexPath);
		}
		private SearcherManager GetSearcherManager(string indexPath)
		{
			SearcherManager result;
			if (SearchEngine.searcherManagers.ContainsKey(indexPath))
			{
				result = SearchEngine.searcherManagers[indexPath];
			}
			else
			{
				result = null;
			}
			return result;
		}
		private void CloseSearcherManager()
		{
			this.CloseSearcherManager(this.indexPath);
		}
		private void CloseSearcherManager(string indexPath)
		{
			if (SearchEngine.searcherManagers.ContainsKey(indexPath))
			{
				SearcherManager searcherManager = null;
				SearchEngine.searcherManagers.TryRemove(indexPath, out searcherManager);
				if (searcherManager != null)
				{
					searcherManager.Close();
				}
			}
		}
		private IndexWriter GetIndexWriter()
		{
			return this.GetIndexWriter(this.indexPath);
		}
		private IndexWriter GetIndexWriter(string indexPath)
		{
			IndexWriter result;
			if (SearchEngine.indexWriters.ContainsKey(indexPath))
			{
				result = SearchEngine.indexWriters[indexPath];
			}
			else
			{
				result = null;
			}
			return result;
		}
		private void CloseIndexWriter()
		{
			this.CloseIndexWriter(this.indexPath);
		}
		private void CloseIndexWriter(string indexPath)
		{
			if (SearchEngine.indexWriters.ContainsKey(indexPath))
			{
				IndexWriter indexWriter = null;
				SearchEngine.indexWriters.TryRemove(indexPath, out indexWriter);
				if (indexWriter != null)
				{
					indexWriter.Close();
					indexWriter.Dispose();
					indexWriter = null;
				}
			}
		}
		private void ReopenSearcher()
		{
			SearcherManager searcherManager = this.GetSearcherManager();
			searcherManager.Reopen();
		}
		private bool IsIndexFileExists()
		{
			return this.IsIndexFileExists(this.indexPath);
		}
		private bool IsIndexFileExists(string indexPath)
		{
			return IndexReader.IndexExists(FSDirectory.Open(new DirectoryInfo(indexPath)));
		}
	}
}
