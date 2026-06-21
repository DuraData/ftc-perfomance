import { describe, it, expect } from 'vitest';

describe('API type mapping and data transformation', () => {
  interface OpmsSubmissionPayload {
    targetId: string;
    quarter: string;
    actual?: number;
    actualDescription?: string;
    varianceReason?: string;
    actualExpenditure?: number;
    dueDate: string;
  }

  interface OpmsSubmissionDto {
    id: string;
    targetId: string;
    quarter: string;
    actual?: number;
    actualDescription?: string;
    varianceReason?: string;
    actualExpenditure?: number;
    dueDate: string;
    status: string;
  }

  const toOpmsSubmissionPayload = (dto: OpmsSubmissionDto): OpmsSubmissionPayload => ({
    targetId: dto.targetId,
    quarter: dto.quarter,
    actual: dto.actual,
    actualDescription: dto.actualDescription,
    varianceReason: dto.varianceReason,
    actualExpenditure: dto.actualExpenditure,
    dueDate: dto.dueDate,
  });

  const toOpmsSubmissionDto = (payload: OpmsSubmissionPayload, id: string, status: string): OpmsSubmissionDto => ({
    id,
    ...payload,
    status,
  });

  it('converts API DTO to payload for submission', () => {
    const dto: OpmsSubmissionDto = {
      id: 'sub-1',
      targetId: 'target-1',
      quarter: 'Q1',
      actual: 100,
      actualDescription: 'Completed successfully',
      actualExpenditure: 5000,
      dueDate: '2026-06-30',
      status: 'submitted',
    };

    const payload = toOpmsSubmissionPayload(dto);

    expect(payload.targetId).toBe('target-1');
    expect(payload.quarter).toBe('Q1');
    expect(payload.actual).toBe(100);
    expect(payload.actualDescription).toBe('Completed successfully');
    expect(payload.actualExpenditure).toBe(5000);
  });

  it('excludes status and id from payload', () => {
    const dto: OpmsSubmissionDto = {
      id: 'sub-1',
      targetId: 'target-1',
      quarter: 'Q1',
      dueDate: '2026-06-30',
      status: 'submitted',
    };

    const payload = toOpmsSubmissionPayload(dto);

    expect('id' in payload).toBe(false);
    expect('status' in payload).toBe(false);
  });

  it('converts payload to DTO for API response mapping', () => {
    const payload: OpmsSubmissionPayload = {
      targetId: 'target-1',
      quarter: 'Q1',
      actual: 100,
      dueDate: '2026-06-30',
    };

    const dto = toOpmsSubmissionDto(payload, 'sub-1', 'submitted');

    expect(dto.id).toBe('sub-1');
    expect(dto.status).toBe('submitted');
    expect(dto.targetId).toBe('target-1');
    expect(dto.quarter).toBe('Q1');
  });

  it('handles optional fields in transformation', () => {
    const minimalPayload: OpmsSubmissionPayload = {
      targetId: 'target-1',
      quarter: 'Q1',
      dueDate: '2026-06-30',
    };

    const payload = toOpmsSubmissionPayload({
      id: 'sub-1',
      ...minimalPayload,
      status: 'draft',
    });

    expect(payload.actual).toBeUndefined();
    expect(payload.actualDescription).toBeUndefined();
    expect(payload.varianceReason).toBeUndefined();
  });

  it('normalizes date formats for API', () => {
    const normalizeDate = (date: Date | string): string => {
      if (typeof date === 'string') return date;
      return date.toISOString().split('T')[0];
    };

    expect(normalizeDate('2026-06-30')).toBe('2026-06-30');
    expect(normalizeDate(new Date('2026-06-30'))).toBe('2026-06-30');
  });

  it('handles null and undefined values in mapping', () => {
    const dto: OpmsSubmissionDto = {
      id: 'sub-1',
      targetId: 'target-1',
      quarter: 'Q1',
      actual: undefined,
      actualDescription: null as any,
      dueDate: '2026-06-30',
      status: 'draft',
    };

    const payload = toOpmsSubmissionPayload(dto);

    expect(payload.actual).toBeUndefined();
    expect(payload.actualDescription).toBeNull();
  });

  it('preserves data integrity through round-trip transformation', () => {
    const originalPayload: OpmsSubmissionPayload = {
      targetId: 'target-123',
      quarter: 'Q2',
      actual: 250,
      actualDescription: 'Q2 target achieved',
      varianceReason: 'Better than expected',
      actualExpenditure: 12500,
      dueDate: '2026-06-30',
    };

    // Convert to DTO
    const dto = toOpmsSubmissionDto(originalPayload, 'sub-1', 'submitted');

    // Convert back to payload
    const roundTripPayload = toOpmsSubmissionPayload(dto);

    expect(roundTripPayload.targetId).toBe(originalPayload.targetId);
    expect(roundTripPayload.quarter).toBe(originalPayload.quarter);
    expect(roundTripPayload.actual).toBe(originalPayload.actual);
    expect(roundTripPayload.actualDescription).toBe(originalPayload.actualDescription);
    expect(roundTripPayload.actualExpenditure).toBe(originalPayload.actualExpenditure);
  });
});
