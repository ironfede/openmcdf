ver 2.0 beta 1 (NOT FOR PRODUCTION)
FIXED: Issues with StreamRW in reading Int64
FIXED: Issue with poor random naming policy for directory invalidation
FIXED: Redundancy of rethreading a RBTree after insertion/removal operations
NOTE: This is a technical preview not aimed to production use.

---------
ver 2.0 pre-release (NOT FOR PRODUCTION)
ADD: Red-Black tree full implementation to speed up large data structure read access (thousands of stream/storage objects)
ADD: Enhanced Stream resizing
ADD: Extensions to use native .net framework Stream object
ADD: Code has been ported to .net 4.0 framework
NOTE: This is a technical preview not aimed to production use.

---------
ver 1.5.4
FIXED: In particular conditions, an opened file could be left opened after a loading exception
FIXED: Circular references of corrupted files could lead to stack overflows
FIXED: Enanched standard compliance: corrupted file loading defaults to abort operation.
ADD: Version property
ADD: New overloaded constructors to force the load of possibly corrrupted files.

---------
ver 1.5.3
ADD: 'GetAllNamedEntries' Method to access structured files without tree-loading performance penalties
ADD: New hex editor for stuctured storage explorer sample application

---------
ver 1.5.2
FIXED: Math error in sector number recognition caused exception when reading some streams
FIXED: Saving twice caused OutOfMemoryException
FIXED: Error when using names of exactly 31 characters for streams or storages

---------
ver: 1.5.1.
FIXED: Casting error when removing uncommitted-added Stream.
ADDED: CFDuplicatedItem exception thrown when trying to add duplicated items (previously item addition was silently failing).

---------
ver: 1.5.0
FIXED: Exception thrown when removing a stream of length equals to zero.

---------
ver: 1.5.0 - RC1
ADD: New Update mode to commit changes to the underlying stream
ADD: Sector recycle to reuse unallocated sectors
ADD: File shrinking to compact compound files
ADD: Support for version 4 of specs (4096 bytes sectors)
ADD: Partial stream data reading to read data from a specified offset
ADD: Advanced lazy loading to reduce memory footprint of application

!! FIXED: CHANGED NAMESPACE to OpenMcdf !!

--------
ver: 1.4.1
FIXED: ERROR, internal modifier applied to Delete method
FIXED: Redundant method call for DIFAT chain

ADD: 'Delete' feature for sample project

--------
ver. 1.4.0
ADD: 'Remove' feature for storage and stream objects.
FIXED: ERROR in manipulation of streams with a length of 4096 bytes (cutoff bug) (Thx to meddingt)
FIXED: ERROR in zero sized streams

--------
ver. 1.3.1
FIXED: Error in DIFAT sectors manipulation

--------
ver. 1.3
FIXED: Null pointer in traversal with empty storages;

--------
ver. 1.2
FIXED: Fixed ministream (<4096 bytes) bug;

--------
ver. 1.1
ADD: Added traversal of Compound file method (VisitEntries);
FIXED: Fixed bug when multiple storage added;

--------
ver. 1.0
Initial release