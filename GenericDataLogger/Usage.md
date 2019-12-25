# Library Purpose
The intent of the library is to aid in the serialization and deserialization of various data types across application instances or multiple versions of the same application. Originally for the writing of data within a games replay system, it also allows one-off serialization of data to be read across real time instances (game network traffic).

## Data Structure
In order to allow an application which may have slightly different version of the same data classes (say, the application has been upgraded but needs to replay old files), or allow an application to send and recieve data without needing different sockets for different data types, the application writes metadata into each data block it creates, which helps reading application identify the correct data type to decode to.

A file header consists of version information and a list of all types that were registered during the writing process. This list is referenced during the write process to get a type ID for each block written out, and by the read to identify the type of block being read.

A block header consists of the type ID, timestamp, and write type for the block, as well as the size of the block to read in for decoding.

## Backing Libraries
The library uses MessagePack-CSharp (https://github.com/neuecc/MessagePack-CSharp) to serialize and deserialize the data. As such, data classes used by calling applications must use the MessagePack attribute tags to identify the structure of data objects being serialized.

# Cached Writer/Reader
A writing system that caches updates in memory until the information is requested to be flushed to a file or another data stream. There are two types of updates that can be written out, a full update or a partial update.

A full update is meant to be the current state of the entire system, without any assumptions about what previous data chunks looked like. This gives the replay system defined points where data is guaranteed to be current, allowing for easy seeks around the data.

A partial update is either a quick data of state change in the system that needs to be logged for continuity purposes, as each full update could be a number of seconds apart. These are intended to be smaller and more localized.

## Usage
### Writing

1. Create an instance of CachedSerializeWriter
2. Register file version for verification purposes
3. Register class types that are updated to the system
4. Update data objects in the cache
5. Push data object updates to the internal buffer
6. Flush the buffer to the file stream

```C#
// Initialize headers
CachedSerializeWriter writer = new CachedSerializeWriter(testOutputFile, true, false);
writer.RegisterVersion(1, 5, 43);
writer.RegisterType(typeof(TestData), BlockDataTypes.Full | BlockDataTypes.Partial);

writer.Update(testData); // Updates the data cache with latest object

writer.WriteBuffer(1000); // Writes the current cache to the buffer, with the timestamp of 1000

writer.FlushToStream(); // Push the buffer to the file stream

writer.Dispose(); // Close everything
```

### Reading

1. Create an instance of CachedSerializeReader
2. Register to the reactive stream for read data callback
3. Read the stream into memory buffer (optional)
4. Read the header data, used to verify file and load type information
5. Read the data from the buffer (until an optional timestamp)

```C#
// Initialize reader
CachedSerializeReader reader = new CachedSerializeReader(testInputFile, true);

// Subscribe to the reader. As data in read, this subscription will trigger (reactive stream)
reader.WhenDataRead.Subscribe(data =>
    {
        var actualData = data.DataBlock as TestData;
    });

reader.ReadFromStream(); // Read all data from the file stream to the data buffer

reader.ReadHeader(); // Read the header information from the buffer

reader.ReadData(); // Read all data from the buffer (use overloads to go to a timestamp)

reader.Dispose(); // Close everything
```

# Direct Writer/Reader
A writing system that writes one set of data to a given stream, using the same header/block format the cached version uses, but with the assumption that both the writer and the reader are using the same registered types, in the same order. This is useful for multiple instances of an application sending data over a network, as the application and registered types should be the same.

## Usage
### Writing

1. Create an instance of DirectSerializeWriter
2. Register types to be available (must be the same between reader and writer)
3. Write to a specified stream

```C#
// Initialize headers
MemoryStream ms = new MemoryStream(); // Empty stream to write to
DirectSerializeWriter writer = new DirectSerializeWriter(false);
writer.RegisterType(typeof(TestData));

writer.Write(ms, testData); // Encode the data to the stream
```

### Reading

1. Create an instance of DirectSerializeReader
2. Register types to be available (must be the same between reader and writer)
3. Read from a specified stream

```C#
// Initialize headers
MemoryStream ms = new MemoryStream(); // Stream with data to decode
DirectSerializeReader reader = new DirectSerializeReader(false);
reader.RegisterType(typeof(TestData));

var retData = reader.Read(ms); // Read the data into a ReadSerializeData object
```