#ifndef __MINIFLT_H__
#define __MINIFLT_H__


//  Enabled warnings

#pragma warning(error:4100)     //  Enable-Unreferenced formal parameter
#pragma warning(error:4101)     //  Enable-Unreferenced local variable
#pragma warning(error:4061)     //  Eenable-missing enumeration in switch statement
#pragma warning(error:4505)     //  Enable-identify dead functions

#include <fltKernel.h>
#include <dontuse.h>
#include <suppress.h>

#include "CommFunc.h"

#define TAG_CONTEXT       'FMTC'
#define MIN_SECTOR_SIZE 0x200

#define PROCESS_TERMINATE 0x1
#define ACTIVE_PROCESS_LINKS 0x2e8

typedef struct _MINIFLT_INFO {
	PCHAR UserName[MAX_NAME];
	PCHAR GroupName[MAX_NAME];
	PCHAR FileName[MAX_KPATH];
	PCHAR ProcName[MAX_KPATH];
} MINIFLT_INFO, *PMINIFLT_INFO;

typedef enum {
	DRIVE_FIXED = 0,
	DRIVE_NETWORK,
	DRIVE_REMOVABLE,
	DRIVE_UNKNOWN
} DRIVE_TYPE, *PDRIVE_TYPE;

typedef struct _VOLUME_CONTEXT {
	UNICODE_STRING VolumeName;
	ULONG SectorSize;

	FLT_FILESYSTEM_TYPE FileSystemType;
	DRIVE_TYPE DriveType;
} VOLUME_CONTEXT, *PVOLUME_CONTEXT;

typedef struct _MYMINIFLT_DATA {
	BOOL bInitialize;
} MYMINIFLT_DATA, *PMYMINIFLT_DATA;

typedef union _FILE_REFERENCE {
	struct {
		ULONGLONG Value;
		ULONGLONG UpperZeroes;
	} FileId64;

	FILE_ID_128 FileId128;
} FILE_REFERENCE, * PFILE_REFERENCE;

ULONGLONG GetFileId(
	_In_ PCFLT_RELATED_OBJECTS FltObjects
);

PMINIFLT_INFO GetMiniFltInfo(
	_In_ PFLT_CALLBACK_DATA Data,
	_In_ PCFLT_RELATED_OBJECTS FltObjects,
	_In_ PCHAR CallFuncName
);

PCHAR MakeFileName(
	_In_ PVOLUME_CONTEXT pVolContext,
	_In_ PFLT_FILE_NAME_INFORMATION NameInfo
);

PCHAR MakeFileNameByFileObj(
	_In_ PVOLUME_CONTEXT	pVolContext,
	_In_ PFILE_OBJECT FileObject
);

PMINIFLT_INFO GetNewMiniFltInfo(
	_In_ PFLT_CALLBACK_DATA Data,
	_In_ PCFLT_RELATED_OBJECTS FltObjects
);



#endif __MINIFLT_H__

