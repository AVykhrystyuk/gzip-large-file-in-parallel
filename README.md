# Parallel GZip for large files

###  Requirements:

Develop a console application for compression/decompression (System.IO.Compression.GzipStream) of files using multithreading.

Application must:
- efficiently parallelize and synchronize chunk processing in a multiprocessor environment
- process large files that exceed RAM available
- use only the basic classes and synchronization primitives (`Thread`, `Manual/AutoResetEvent`, `Monitor`, `Semaphor`, `Mutex`). It is NOT allowed to use `async/await`, `ThreadPool`, `BackgroundWorker` or `TPL`
- inform the user with a clear message in case of exceptional situations
- be written using OOP principles
- have the folowing CLI interface `GZipTest.exe compress/decompress [source filepath] [destination filepath]`
- in case of success return 0, on error return 1
