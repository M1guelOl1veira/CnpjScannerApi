const companyCnpj = "12.345.678/0001-90";
const unrelated = 123;
const taxId = 987654321;
let myVar: number = 123;
var legacy = 789;

class Business {
  public cnpjCode: string = "00.000.000/0000-00";
  private internalTaxNumber = 112233445566;
  public id: number = 123;
  private cnpjNumber = 12345678000190; // inferred as number
  protected taxId!: number;
  
  get taxGetter(): number {
    return 9988776655;
  }
}

interface ICompany {
  taxField: number;
  name: string;
}

enum TaxEnum {
  TaxValue = 12345678000190,
  OtherValue = 100
}

function registerCompany(cnpj: string, name: string, taxId: number) {
  const normalizedTax = taxId.toString();
  return normalizedTax;
}
