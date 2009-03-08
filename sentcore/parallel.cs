// Parallel for loop
// It is expected that this will be replaced by native Parallel.for within Mono at some point
//
// Originally from AForge Core Library (GPL licensed)
// AForge.NET framework
//
// Copyright © Andrew Kirillov, 2008
// andrew.kirillov@gmail.com
//
// Copyright © Israel Lot, 2008
// israel.lot@gmail.com


using System;
using System.Threading;

namespace sentience.core
{
    public sealed class Parallel
    {
        /// <summary>
        /// Delegate defining for-loop's body.
        /// </summary>
        /// <param name="index">Loop's index</param>
        public delegate void ForLoopBody(int index);

        // number of threads used for parallelism
        private static int threadsCount = System.Environment.ProcessorCount;

        // synchronization object
        private static object sync = new Object();

        // single instance of the class to implement singleton pattern
        private static volatile Parallel instance = null;
        
        // threads to be used
        private Thread[] threads = null;

        // events to signal about job availability and thread availability
        private AutoResetEvent[] jobAvailable = null;
        
        // which threads are currently idle
        private ManualResetEvent[] threadIdle = null;

        // loop's body and its current and stop index
        private int currentIndex;
        private int stopIndex;
        private ForLoopBody loopBody;

        /// <summary>
        /// get or set the number of threads to be used
        /// </summary>
        public static int ThreadsCount
        {
            get { return threadsCount; }
            set
            {
                lock ( sync )
                {
                    threadsCount = Math.Max( 1, value );
                }
            }
        }

        /// <summary>
        /// Parallel for loop - we don't cate what order the itterations are run in
        /// </summary>
        /// <param name="start">start index.</param>
        /// <param name="stop">stop index.</param>
        /// <param name="loopBody">Loop's body</param>
        /// <example>
        /// Parallel.For( 0, 20, delegate( int i )
        /// {
        ///     // insert your itteration code here...
        /// } );
        /// </example>
        public static void For( int start, int stop, ForLoopBody loopBody  )
        {
            lock ( sync )
            {
                // get instance of parallel computation manager
                Parallel instance = Instance;

                instance.currentIndex   = start - 1;
                instance.stopIndex      = stop;
                instance.loopBody       = loopBody;

                // signal about available job for all threads and mark them busy
                for ( int i = 0; i < threadsCount; i++ )
                {
                    instance.threadIdle[i].Reset();
                    instance.jobAvailable[i].Set();
                }

                // wait until all threads become idle
                for ( int i = 0; i < threadsCount; i++ )
                {
                    instance.threadIdle[i].WaitOne( );
                }
            }
        }

        // Private constructor to avoid class instantiation
        private Parallel( ) { }

        // Get instace of the Parallel class
        private static Parallel Instance
        {
            get
            {
                if ( instance == null )
                {
                    instance = new Parallel( );
                    instance.Initialize( );
                }
                else
                {
                    if ( instance.threads.Length != threadsCount )
                    {
                        // terminate old threads
                        instance.Terminate();
                        
                        // reinitialize
                        instance.Initialize();
                    }
                }
                return instance;
            }
        }

        /// <summary>
        /// Initialize Parallel class's instance creating required number of threads 
        /// and synchronization objects
        /// </summary>
        private void Initialize( )
        {
            // array of events, which signal about available job
            jobAvailable = new AutoResetEvent[threadsCount];

            // array of events, which signal about available thread
            threadIdle = new ManualResetEvent[threadsCount];

            // array of threads
            threads = new Thread[threadsCount];

            for ( int i = 0; i < threadsCount; i++ )
            {
                jobAvailable[i] = new AutoResetEvent( false );
                threadIdle[i]   = new ManualResetEvent( true );

                threads[i] = new Thread( new ParameterizedThreadStart( WorkerThread ) );
                threads[i].IsBackground = true;
                threads[i].Start( i );
            }
        }

        /// <summary>
        /// Terminate all worker threads used for parallel computations and close all synchronization objects
        /// </summary>
        private void Terminate( )
        {
            // finish thread by setting null loop body and signaling about available work
            loopBody = null;
            for ( int i = 0, threadsCount = threads.Length ; i < threadsCount; i++ )
            {
                jobAvailable[i].Set( );

                // wait for thread termination
                threads[i].Join( );

                // close events
                jobAvailable[i].Close( );
                threadIdle[i].Close( );
            }

            // clean all array references
            jobAvailable    = null;
            threadIdle      = null;
            threads         = null;
        }

        /// <summary>
        /// Worker thread performing parallel computations in loop
        /// </summary>
        /// <param name="index">index number of the thread</param>
        private void WorkerThread(object index)
        {
            int threadIndex = (int) index;
            int localIndex = 0;

            while (true)
            {
                // wait until there is job to do
                jobAvailable[threadIndex].WaitOne();

                // exit on null body
                if ( loopBody == null )
                    break;

                while ( true )
                {
                    // get local index incrementing global loop's current index
                    localIndex = Interlocked.Increment(ref currentIndex);

                    if (localIndex >= stopIndex)
                        break;

                    // run loop's body
                    loopBody(localIndex);
                }

                // signal about thread availability
                threadIdle[threadIndex].Set();
            }
        }
    }
}
