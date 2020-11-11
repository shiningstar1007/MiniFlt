#ifndef __SYSTEMCALLHOOK_H__
#define __SYSTEMCALLHOOK_H__

#include "CommFunc.h"

extern PVOID g_RegHandle;
OB_PREOP_CALLBACK_STATUS ObPreCallBack(
	_In_ PVOID RegContext,
	_Inout_ POB_PRE_OPERATION_INFORMATION ObPreOperInfo
);

VOID ObPostCallBack(
	_In_ PVOID RegContext,
	_Inout_ POB_POST_OPERATION_INFORMATION OperInfo
);

NTSTATUS StartProtectProcess();
VOID StopProtectProcess();

#define MAX_REGPATH 1024

typedef struct _REG_ROOTKEYW {
	WCHAR KRootName[MAX_KPATH];
	WCHAR URootName[32];
	ULONG KRootLen;
} REG_ROOTKEYW, * PREG_ROOTKEYW;

typedef struct _REG_LINKKEYW {
	WCHAR OriName[MAX_KPATH];
	WCHAR LinkName[MAX_KPATH];
	ULONG OriLen;
} REG_LINKKEYW, * PREG_LINKKEYW;

typedef struct _REG_CTRLKEYW {
	WCHAR KeyName[MAX_KPATH];
	ULONG NameLen;
} REG_CTRLKEYW, * PREG_CTRLKEYW;

extern LARGE_INTEGER g_RegisterCookie;

#endif