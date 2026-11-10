#!/usr/bin/env python3
"""Validate local measurements and generate README tables from unrounded reports."""
import argparse,json,math,statistics
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
KIND='json'
EXPECTED_CASES=22 if KIND=='json' else 4
ZERO_METHODS={'JsonPopulator_Class_Reused_Interned','JsonPopulator_Struct_Ref','Gate_JsonPopulator_FastList_Reused_Interned','Gate_JsonPopulator_FastList_Reused_Interned_Struct','Standard_Serialize_Writer','JsonPushulator_Class','JsonPushulator_Struct','Manual_Utf8JsonWriter_Direct','Route10kMessages_JsonReader','Route10kMessages_Custom','Route10kMessages_Regex','PopulatorReusedInterned','SystemTextJsonGenerated','PushulatorReusedWriter'}

MEASUREMENT_ITERATIONS=15
WARMUP_ITERATIONS=5
METHOD_SETS={
    'PopulatorBenchmarks': {'Standard_Deserialize_Class','JsonPopulator_Class_Reused_Interned','JsonPopulator_Struct_Ref','Gate_Standard_Deserialize_List','Gate_JsonPopulator_FastList_Reused_Interned','Gate_JsonPopulator_FastList_Reused_Interned_Struct'},
    'PushulatorBenchmarks': {'Standard_Serialize_Class','Standard_Serialize_Writer','JsonPushulator_Class','JsonPushulator_Struct','Manual_Utf8JsonWriter_Direct'},
    'RouterBenchmarks': {'Route10kMessages_JsonReader','Route10kMessages_Custom','Route10kMessages_Regex'},
    'JsonParsingBenchmarks': {'SystemTextJson','SystemTextJsonGenerated','PopulatorNewObject','PopulatorReusedObject','PopulatorReusedInterned'},
    'JsonWritingBenchmarks': {'SystemTextJson','SystemTextJsonGenerated','PushulatorReusedWriter'},
}
EXPECTED_IDENTITIES=({(typename,method,'') for typename,methods in METHOD_SETS.items() for method in methods} if KIND=='json' else
    {('WebSocketRoundtripBenchmarks','EchoRoundtrip',f'PayloadBytes={size}&StandardClient={standard}') for size in (128,4096) for standard in ('False','True')})

def reports(directory):
    """Require every case and finite measured statistics, excluding the other library."""
    cases=[]
    for path in sorted((directory/'bdn/results').glob('*-full-compressed.json')):
        data=json.loads(path.read_text(encoding='utf-8'))
        for case in data['Benchmarks']:
            if ('WebSocket' in case['Type']) != (KIND=='websocket'):continue
            required_job=('IterationCount=15','IterationTime=500ms','LaunchCount=1','WarmupCount=5')
            if any(parameter not in case['DisplayInfo'] for parameter in required_job):
                raise ValueError(f'Wrong benchmark configuration: {case["DisplayInfo"]}')
            workload=[m for m in case['Measurements'] if m['IterationMode']=='Workload']
            actual=[m for m in workload if m['IterationStage']=='Actual']
            warmup=[m for m in workload if m['IterationStage']=='Warmup']
            if len(actual)!=MEASUREMENT_ITERATIONS or len(warmup)!=WARMUP_ITERATIONS or {m['LaunchIndex'] for m in actual}!={1}:
                raise ValueError(f'Incomplete warmup/measurement iterations: {case["FullName"]}')
            stats=case['Statistics']
            if not stats or stats['N']<10 or not math.isfinite(stats['Mean']) or stats['Mean']<=0:
                raise ValueError(f'Incomplete measured case: {case["FullName"]}')
            allocated=case['Memory']['BytesAllocatedPerOperation']
            zero=case['Method'] in ZERO_METHODS and not (case['Type']=='JsonParsingBenchmarks' and case['Method']=='SystemTextJsonGenerated')
            if zero and allocated!=0:raise ValueError(f'Expected 0 B/op: {case["FullName"]}: {allocated}')
            cases.append(case)
    if len(cases)!=EXPECTED_CASES:raise ValueError(f'Expected {EXPECTED_CASES} cases; found {len(cases)}')
    identities=[(c['Type'],c['Method'],c['Parameters']) for c in cases]
    if len(set(identities))!=len(identities) or set(identities)!=EXPECTED_IDENTITIES:
        raise ValueError('Missing, duplicate or unexpected benchmark identities')
    return cases

def streaming(directory):
    """Validate healthy profiles, sample arithmetic, elapsed rates and byte counts."""
    groups={}
    for client in ('standard','pipeline'):
        runs=[]
        for n in range(1,4):
            path=directory/f'websocket-{client}-{n}.json'; run=json.loads(path.read_text(encoding='utf-8'))
            if not (run['ClientType']==client and run['ClientCount']==10 and run['WarmupSeconds']==5 and run['DurationSeconds']>=30 and run['PayloadBytes']==87 and run['FramesPerBatch']==20 and run['BatchBytes']==1780):raise ValueError(f'Unexpected streaming profile: {path}')
            max_snapshot_skew_bytes=2*run['ClientCount']*run['PayloadBytes']
            if run['Messages']<=0 or abs(run['Bytes']-run['Messages']*run['PayloadBytes'])>max_snapshot_skew_bytes:raise ValueError(f'Invalid message bytes: {path}')
            if sum(s['Messages'] for s in run['Samples'])!=run['Messages'] or sum(s['Bytes'] for s in run['Samples'])!=run['Bytes']:raise ValueError(f'Invalid samples: {path}')
            if not math.isclose(run['Messages']/run['DurationSeconds'],run['MessagesPerSecond'],rel_tol=1e-12):raise ValueError(f'Invalid elapsed rate: {path}')
            log=path.with_suffix('.log').read_text(encoding='utf-8')
            if 'Avg Msg/s:' not in log or 'Streaming benchmark failed:' in log:raise ValueError(f'Failed workload: {path}')
            previous_elapsed=0
            for sample in run['Samples']:
                interval=sample['ElapsedSeconds']-previous_elapsed
                if interval<=0 or not math.isclose(sample['Messages']/interval,sample['MessagesPerSecond'],rel_tol=1e-10):
                    raise ValueError(f'Invalid sample elapsed rate: {path}')
                if abs(sample['Bytes']-sample['Messages']*run['PayloadBytes'])>max_snapshot_skew_bytes:raise ValueError(f'Invalid sample payload counters: {path}')
                previous_elapsed=sample['ElapsedSeconds']
            if not math.isclose(previous_elapsed,run['DurationSeconds'],rel_tol=1e-12):raise ValueError(f'Incomplete sample duration: {path}')
            if run['Topology']!='loopback' or run['CpuScope']!='generator-and-clients':raise ValueError(f'Invalid loopback measurement scope: {path}')
            if not math.isclose(run['CpuMilliseconds']*1_000_000/run['Messages'],run['CpuMillisecondsPerMillionMessages'],rel_tol=1e-12):raise ValueError(f'Invalid CPU normalization: {path}')
            runs.append(run)
        groups[client]={'messagesPerSecond':statistics.median(r['MessagesPerSecond'] for r in runs),'allocatedBytesPerMillionMessages':statistics.median(r['AllocatedBytes']*1_000_000/r['Messages'] for r in runs),'cpuMsPerMillionMessages':statistics.median(r['CpuMillisecondsPerMillionMessages'] for r in runs),'gcCounts':[[r[f'Gen{g}Collections'] for g in range(3)] for r in runs],'payloadCounterSkewBytes':[r['Bytes']-r['Messages']*r['PayloadBytes'] for r in runs]}
    return groups

def duration(ns):
    """Display nanoseconds without changing the ratios computed from raw values."""
    scale,unit=(1_000_000,'ms') if ns>=1_000_000 else (1_000,'µs') if ns>=1_000 else (1,'ns')
    return f'{ns/scale:.2f} {unit}'

# Display labels describe the workload; raw reports retain exact benchmark identifiers.
METHOD_LABELS = {
    'Standard_Deserialize_Class': ('System.Text.Json · new object', 'System.Text.Json · новый объект'),
    'JsonPopulator_Class_Reused_Interned': ('JsonPopulator · reused object', 'JsonPopulator · готовый объект'),
    'JsonPopulator_Struct_Ref': ('JsonPopulator · struct (ref)', 'JsonPopulator · структура (ref)'),
    'Gate_Standard_Deserialize_List': ('System.Text.Json · new contract list', 'System.Text.Json · новый список контрактов'),
    'Gate_JsonPopulator_FastList_Reused_Interned': ('JsonPopulator · FastList<class>', 'JsonPopulator · FastList<class>'),
    'Gate_JsonPopulator_FastList_Reused_Interned_Struct': ('JsonPopulator · FastList<struct>', 'JsonPopulator · FastList<struct>'),
    'Standard_Serialize_Class': ('JsonSerializer → new byte[]', 'JsonSerializer → новый byte[]'),
    'Standard_Serialize_Writer': ('JsonSerializer → reused writer', 'JsonSerializer → готовый writer'),
    'JsonPushulator_Class': ('JsonPushulator · class', 'JsonPushulator · класс'),
    'JsonPushulator_Struct': ('JsonPushulator · struct', 'JsonPushulator · структура'),
    'Manual_Utf8JsonWriter_Direct': ('Utf8JsonWriter · manual', 'Utf8JsonWriter · ручная запись'),
    'Route10kMessages_JsonReader': ('Utf8JsonReader', 'Utf8JsonReader'),
    'Route10kMessages_Custom': ('Span.IndexOf', 'Span.IndexOf'),
    'Route10kMessages_Regex': ('Regex', 'Regex'),
    'SystemTextJson': ('System.Text.Json', 'System.Text.Json'),
    'SystemTextJsonGenerated': ('System.Text.Json · generated', 'System.Text.Json · генерация кода'),
    'PopulatorNewObject': ('JsonPopulator · new object', 'JsonPopulator · новый объект'),
    'PopulatorReusedObject': ('JsonPopulator · reused object', 'JsonPopulator · готовый объект'),
    'PopulatorReusedInterned': ('JsonPopulator · reuse + interning', 'JsonPopulator · объект + интернер'),
    'PushulatorReusedWriter': ('JsonPushulator → reused writer', 'JsonPushulator → готовый writer'),
}
SUITE_TEXT = {
    'PopulatorBenchmarks': (
        'JSON population', 'Разбор JSON',
        'One trade update and a list of 592 contracts. Objects, collection capacity and interned symbols are prepared before timing.',
        'Одна сделка и список из 592 контрактов. Объекты, ёмкость коллекций и интернированные символы подготовлены до измерения.',
        'The reused-object and FastList cases use warmed interning. The trade class includes Symbol; the struct omits it. Speedup uses a separate System.Text.Json baseline for one object and for the contract list.',
        'Готовый объект и FastList используют прогретое интернирование. В классе сделки есть Symbol, в структуре его нет. Ускорение считается относительно своего System.Text.Json-варианта для одного объекта и для списка контрактов.'),
    'PushulatorBenchmarks': (
        'JSON writing', 'Запись JSON',
        'Fresh output arrays, reusable writers and direct field-by-field writing. Class and struct models match the population workloads above.',
        'Новый выходной массив, готовые writer-буферы и ручная запись полей. Класс и структура соответствуют моделям разбора выше.',
        'Writer speedup is relative to JsonSerializer with a reused writer; the fresh-array row uses itself as baseline. Caller-owned buffers keep all reused-writer variants at 0 B/op.',
        'Ускорение writer-вариантов считается относительно JsonSerializer с готовым writer; строка нового массива сравнивается с собой. Повторное использование выходной памяти даёт 0 B/op во всех writer-вариантах.'),
    'RouterBenchmarks': (
        'Symbol routing from UTF-8', 'Маршрутизация символов из UTF-8',
        'Look up the symbol in each compact message without building an object tree. The workload covers 5,000 symbols; values below are per message.',
        'Поиск символа в компактном сообщении без построения дерева объектов. В нагрузке 5000 символов; время и память ниже указаны на одно сообщение.',
        'Span.IndexOf searches the compact ASCII symbol field. The accepted message shape and multi-segment behavior are described in the architecture notes.',
        'Span.IndexOf ищет компактное ASCII-поле symbol. Допустимый формат сообщения и обработка нескольких сегментов описаны в архитектуре.'),
    'JsonParsingBenchmarks': (
        'Same-model parsing', 'Разбор одинаковой модели',
        'All variants read the same seven-field trade, including Symbol. These rows separate object reuse from string interning.',
        'Все варианты читают одну модель сделки из семи полей, включая Symbol. Здесь отдельно виден эффект переиспользования объекта и интернирования строк.',
        'Reusing only the object still allocates its Symbol string. Reusing the object and an already interned symbol reaches 0 B/op.',
        'При повторном использовании одного объекта строка Symbol всё ещё создаётся заново. Готовый объект и уже известный интернеру символ дают 0 B/op.'),
    'JsonWritingBenchmarks': (
        'Same-model writing', 'Запись одинаковой модели',
        'The same trade, output buffer and writer lifetime, including the source-generated System.Text.Json baseline.',
        'Одна модель сделки и одинаковый жизненный цикл writer и выходного буфера, включая System.Text.Json с генерацией кода.',
        'The source-generated framework writer is fastest here; all three variants allocate 0 B/op. Population of existing objects and reusable collections is covered separately above.',
        'В этом сценарии стандартный writer с генерацией кода быстрее; все три варианта дают 0 B/op. Заполнение готовых объектов и коллекций показано отдельно выше.'),
}

def render(cases, russian):
    """Render every measured case with workload labels and unchanged numeric baselines."""
    lookup = {(case['Type'], case['Method']): case for case in cases}
    routing = lookup['RouterBenchmarks', 'Route10kMessages_JsonReader']['Statistics']['Mean'] / lookup['RouterBenchmarks', 'Route10kMessages_Custom']['Statistics']['Mean']
    parser_bytes = lookup['PopulatorBenchmarks', 'JsonPopulator_Class_Reused_Interned']['Memory']['BytesAllocatedPerOperation']
    writer_bytes = lookup['PushulatorBenchmarks', 'JsonPushulator_Class']['Memory']['BytesAllocatedPerOperation']
    highlight = (f'**Объект + интернирование: {parser_bytes:g} B/op · Готовый writer: {writer_bytes:g} B/op · Маршрутизация: {routing:.2f}×.**' if russian else
                 f'**Reuse + interning: {parser_bytes:g} B/op · Reused writer: {writer_bytes:g} B/op · Routing: {routing:.2f}×.**')
    blocks = [highlight, '']
    for typename in METHOD_SETS:
        group = [case for case in cases if case['Type'] == typename]
        means = {case['Method']: case['Statistics']['Mean'] for case in group}
        text = SUITE_TEXT[typename]
        heading = '| Вариант | Время | Погрешность | Аллокации | Ускорение |' if russian else '| Method | Mean | Error | Allocated | Speedup |'
        blocks.extend(['### ' + text[int(russian)], '', text[2 + int(russian)], '', heading, '|---|---:|---:|---:|---:|'])
        for case in group:
            method = case['Method']
            if typename == 'PopulatorBenchmarks':
                baseline = 'Gate_Standard_Deserialize_List' if method.startswith('Gate_') else 'Standard_Deserialize_Class'
            elif typename == 'PushulatorBenchmarks':
                baseline = 'Standard_Serialize_Class' if method == 'Standard_Serialize_Class' else 'Standard_Serialize_Writer'
            elif typename == 'RouterBenchmarks':
                baseline = 'Route10kMessages_JsonReader'
            else:
                baseline = 'SystemTextJson'
            stats = case['Statistics']
            label = METHOD_LABELS[method][int(russian)]
            allocated = case['Memory']['BytesAllocatedPerOperation']
            blocks.append(f'| `{label}` | {duration(stats["Mean"])} | {duration(stats["ConfidenceInterval"]["Margin"])} | {allocated:g} B | {means[baseline] / stats["Mean"]:.2f}× |')
        blocks.extend(['', text[4 + int(russian)], ''])
    return '\n'.join(blocks)

def render_local(cases, streams, russian):
    """Keep the README numeric region reproducible from the JSON reports."""
    return render(cases, russian)

def main():
    """Regenerate both languages and retain comparison statistics in machine-readable form."""
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('directory',type=Path);args=parser.parse_args();directory=args.directory.resolve()
    cases=reports(directory);before=reports(directory/'baseline') if (directory/'baseline').exists() else []
    key=lambda c:(c['FullName'],c['Parameters'])
    old={key(c):c for c in before}
    if before and set(old)!={key(c) for c in cases}:raise ValueError('Before/after case identities differ')
    summary={'caseCount':len(cases),'comparison':[{'name':c['FullName'],'parameters':c['Parameters'],'beforeMeanNs':old[key(c)]['Statistics']['Mean'],'afterMeanNs':c['Statistics']['Mean'],'afterToBefore':c['Statistics']['Mean']/old[key(c)]['Statistics']['Mean'],'beforeAllocatedBytes':old[key(c)]['Memory']['BytesAllocatedPerOperation'],'afterAllocatedBytes':c['Memory']['BytesAllocatedPerOperation']} for c in cases if key(c) in old]}
    if KIND=='websocket':
        summary['streaming']=streaming(directory)
        if before:summary['baselineStreaming']=streaming(directory/'baseline')
    (directory/'summary.json').write_text(json.dumps(summary,indent=2)+'\n', encoding='utf-8')
    for filename,russian in [('README.md',False),('README.ru.md',True)]:
        p=ROOT/filename;s=p.read_text(encoding='utf-8');start='<!-- LOCAL_RESULTS_START -->';end='<!-- LOCAL_RESULTS_END -->';head,rest=s.split(start);_,tail=rest.split(end)
        table=render_local(cases,summary.get('streaming'),russian)
        rendered=head+start+'\n'+table+'\n'+end+tail
        if KIND=='websocket':
            eth_start='<!-- ETHERNET_RESULTS_START -->';eth_end='<!-- ETHERNET_RESULTS_END -->'
            before_eth,eth_rest=rendered.split(eth_start);_,after_eth=eth_rest.split(eth_end)
            rendered=before_eth+eth_start+'\n'+render_ethernet(russian)+'\n'+eth_end+after_eth
        p.write_text(rendered, encoding='utf-8')
    print(f'Validated {len(cases)} measured cases'+(' and before/after comparison' if before else '')+(' plus streaming' if KIND=='websocket' else '')+'; generated both READMEs.')
if __name__=='__main__':main()
